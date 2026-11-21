namespace server.domain.chat;

/// <summary>
/// スレッド画像の保存先（本番・ステージングは Cloudflare R2、ローカルはディスク）。
/// </summary>
public interface IThreadImageStorage
{
    /// <summary>保存先が設定されていて、画像を受け付けられるか。</summary>
    bool IsAvailable { get; }

    Task SaveAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>画像を削除する。公開 URL のキャッシュもあわせて消す。存在しないキーはエラーにしない。</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>表示用の URL。保存先が設定されていなければ null。</summary>
    string? GetPublicUrl(string key);
}
