using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using server.domain.chat;

namespace server.infrastructure.chat.images;

/// <summary>
/// Cloudflare R2（S3 互換 API）に画像を保存する。
/// 表示は公開バケットに紐付けたカスタムドメイン（PublicBaseUrl）から直接配信し、サーバーは経由しない。
/// 削除時は Cloudflare のキャッシュもパージする（CachePurgeZoneId / CachePurgeApiToken が設定されている場合）。
/// </summary>
public sealed class R2ThreadImageStorage(
    HttpClient httpClient,
    ThreadImageStorageSettings settings,
    TimeProvider timeProvider,
    ILogger<R2ThreadImageStorage> logger) : IThreadImageStorage
{
    private const string Region = "auto";
    private const string Service = "s3";

    // キーは毎回新しい GUID なので、内容が変わることはない
    private const string CacheControl = "public, max-age=31536000, immutable";

    public bool IsAvailable => settings.IsR2Configured;

    public async Task SaveAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        EnsureValidKey(key);
        using var request = CreateSignedRequest(HttpMethod.Put, key, content, new Dictionary<string, string>
        {
            ["content-type"] = contentType,
            ["cache-control"] = CacheControl
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"R2 への画像の保存に失敗しました（{(int)response.StatusCode}）: {body}");
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureValidKey(key);
        using (var request = CreateSignedRequest(HttpMethod.Delete, key, [], new Dictionary<string, string>()))
        using (var response = await httpClient.SendAsync(request, cancellationToken))
        {
            // 存在しないキーの削除も S3 互換 API は 204 を返す
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"R2 の画像の削除に失敗しました（{(int)response.StatusCode}）: {body}");
            }
        }

        await PurgeCacheAsync(key, cancellationToken);
    }

    public string? GetPublicUrl(string key) => IsAvailable ? settings.BuildPublicUrl(key) : null;

    private HttpRequestMessage CreateSignedRequest(
        HttpMethod method,
        string key,
        byte[] content,
        Dictionary<string, string> extraHeaders)
    {
        var host = $"{settings.R2AccountId}.r2.cloudflarestorage.com";
        var canonicalPath = "/" + S3RequestSigner.EncodePath($"{settings.R2BucketName}/{key}");
        var payloadHash = content.Length == 0
            ? S3RequestSigner.EmptyPayloadHash
            : S3RequestSigner.HashHex(content);
        var now = timeProvider.GetUtcNow();

        var signedHeaders = new Dictionary<string, string>(extraHeaders)
        {
            ["host"] = host,
            ["x-amz-content-sha256"] = payloadHash,
            ["x-amz-date"] = S3RequestSigner.FormatAmzDate(now)
        };

        var authorization = S3RequestSigner.CreateAuthorization(
            method.Method,
            canonicalPath,
            signedHeaders,
            payloadHash,
            settings.R2AccessKeyId!,
            settings.R2SecretAccessKey!,
            Region,
            Service,
            now);

        var request = new HttpRequestMessage(method, new Uri($"https://{host}{canonicalPath}"));
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        request.Headers.TryAddWithoutValidation("x-amz-date", signedHeaders["x-amz-date"]);

        if (method == HttpMethod.Put)
        {
            request.Content = new ByteArrayContent(content);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(extraHeaders["content-type"]);
            request.Headers.TryAddWithoutValidation("cache-control", extraHeaders["cache-control"]);
        }

        return request;
    }

    private async Task PurgeCacheAsync(string key, CancellationToken cancellationToken)
    {
        if (!settings.IsCachePurgeConfigured)
        {
            return;
        }

        var url = settings.BuildPublicUrl(key);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.cloudflare.com/client/v4/zones/{settings.CachePurgeZoneId}/purge_cache");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.CachePurgeApiToken);
        request.Content = JsonContent.Create(new { files = new[] { url } });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            // 画像自体は消えているので、パージの失敗はログだけ残す（キャッシュは期限切れで消える）
            logger.LogWarning(
                "Cloudflare のキャッシュのパージに失敗しました（{StatusCode}）: {Url} {Body}",
                (int)response.StatusCode,
                url,
                body);
        }
    }

    private static void EnsureValidKey(string key)
    {
        if (!ThreadImage.IsValidKey(key))
        {
            throw new ArgumentException("画像キーの形式が正しくありません。", nameof(key));
        }
    }
}
