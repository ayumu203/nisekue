using server.domain.chat;

namespace server.infrastructure.chat.images;

/// <summary>保存先が設定されていないとき。画像の投稿は受け付けず、既存の画像の URL も返さない。</summary>
public sealed class UnavailableThreadImageStorage : IThreadImageStorage
{
    public bool IsAvailable => false;

    public Task SaveAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("画像の保存先が設定されていません。");

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public string? GetPublicUrl(string key) => null;
}
