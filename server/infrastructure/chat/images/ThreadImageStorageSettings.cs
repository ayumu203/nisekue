using Microsoft.Extensions.Configuration;

namespace server.infrastructure.chat.images;

/// <summary>
/// スレッド画像の保存先の設定（appsettings / 環境変数の ThreadImageStorage セクション）。
/// R2 の4項目と PublicBaseUrl がそろっていれば R2 に保存する。
/// そろっていなくて UseLocal が true ならローカルのディスクに保存する（ローカル開発用。launchSettings.json で有効化）。
/// どちらでもなければ画像の投稿は受け付けない（画像なしのスレッドは作れる）。
/// </summary>
public sealed record ThreadImageStorageSettings(
    string? R2AccountId,
    string? R2AccessKeyId,
    string? R2SecretAccessKey,
    string? R2BucketName,
    string? PublicBaseUrl,
    string? CachePurgeZoneId,
    string? CachePurgeApiToken,
    bool UseLocal,
    string? LocalDirectory)
{
    public const string SectionName = "ThreadImageStorage";

    public bool IsR2Configured =>
        !string.IsNullOrWhiteSpace(R2AccountId)
        && !string.IsNullOrWhiteSpace(R2AccessKeyId)
        && !string.IsNullOrWhiteSpace(R2SecretAccessKey)
        && !string.IsNullOrWhiteSpace(R2BucketName)
        && !string.IsNullOrWhiteSpace(PublicBaseUrl);

    public bool IsCachePurgeConfigured =>
        !string.IsNullOrWhiteSpace(CachePurgeZoneId) && !string.IsNullOrWhiteSpace(CachePurgeApiToken);

    public static ThreadImageStorageSettings FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        return new ThreadImageStorageSettings(
            R2AccountId: section["R2AccountId"],
            R2AccessKeyId: section["R2AccessKeyId"],
            R2SecretAccessKey: section["R2SecretAccessKey"],
            R2BucketName: section["R2BucketName"],
            PublicBaseUrl: section["PublicBaseUrl"],
            CachePurgeZoneId: section["CachePurgeZoneId"],
            CachePurgeApiToken: section["CachePurgeApiToken"],
            UseLocal: section.GetValue("UseLocal", defaultValue: false),
            LocalDirectory: section["LocalDirectory"]);
    }

    /// <summary>公開 URL を作る。PublicBaseUrl の末尾の / は無視する。</summary>
    public string BuildPublicUrl(string key) => $"{(PublicBaseUrl ?? string.Empty).TrimEnd('/')}/{key}";
}
