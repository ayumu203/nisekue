using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace server.infrastructure.chat.images;

/// <summary>
/// S3 互換 API（Cloudflare R2）向けの AWS Signature Version 4 の署名を作る。
/// SDK を入れずに PUT / DELETE だけ使うため自前で実装している。
/// </summary>
public static class S3RequestSigner
{
    public const string Algorithm = "AWS4-HMAC-SHA256";
    public const string EmptyPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    /// <summary>
    /// Authorization ヘッダーの値を返す。
    /// headers には署名に含めるヘッダー（host, x-amz-content-sha256, x-amz-date を必ず含める）を渡す。
    /// </summary>
    public static string CreateAuthorization(
        string method,
        string canonicalPath,
        IReadOnlyDictionary<string, string> headers,
        string payloadHash,
        string accessKeyId,
        string secretAccessKey,
        string region,
        string service,
        DateTimeOffset now)
    {
        var amzDate = FormatAmzDate(now);
        var date = amzDate[..8];

        var sortedHeaders = headers
            .Select(x => new KeyValuePair<string, string>(x.Key.Trim().ToLowerInvariant(), x.Value.Trim()))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ToArray();
        var canonicalHeaders = string.Concat(sortedHeaders.Select(x => $"{x.Key}:{x.Value}\n"));
        var signedHeaders = string.Join(';', sortedHeaders.Select(x => x.Key));

        var canonicalRequest = string.Join('\n',
            method,
            canonicalPath,
            string.Empty,
            canonicalHeaders,
            signedHeaders,
            payloadHash);

        var scope = $"{date}/{region}/{service}/aws4_request";
        var stringToSign = string.Join('\n',
            Algorithm,
            amzDate,
            scope,
            HashHex(Encoding.UTF8.GetBytes(canonicalRequest)));

        var signingKey = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secretAccessKey), date);
        signingKey = HmacSha256(signingKey, region);
        signingKey = HmacSha256(signingKey, service);
        signingKey = HmacSha256(signingKey, "aws4_request");
        var signature = Convert.ToHexStringLower(HmacSha256(signingKey, stringToSign));

        return $"{Algorithm} Credential={accessKeyId}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}";
    }

    public static string FormatAmzDate(DateTimeOffset now) =>
        now.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    public static string HashHex(byte[] payload) => Convert.ToHexStringLower(SHA256.HashData(payload));

    /// <summary>パスの各セグメントを RFC 3986 の非予約文字以外エンコードする（/ は区切りとして残す）。</summary>
    public static string EncodePath(string path) =>
        string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    private static byte[] HmacSha256(byte[] key, string data) =>
        HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));
}
