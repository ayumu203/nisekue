using server.domain.chat;

namespace server.infrastructure.chat.images;

/// <summary>
/// ローカル開発用。画像をディスクに保存し、サーバーの /thread-images/{key} から配信する。
/// 設定 ThreadImageStorage:UseLocal が true で、R2 が設定されていないときだけ使う。
/// </summary>
public sealed class LocalThreadImageStorage(ThreadImageStorageSettings settings) : IThreadImageStorage
{
    public const string RoutePrefix = "/thread-images";

    private readonly string _rootDirectory = string.IsNullOrWhiteSpace(settings.LocalDirectory)
        ? Path.Combine(Path.GetTempPath(), "nisekue-thread-images")
        : settings.LocalDirectory;

    public bool IsAvailable => true;

    public async Task SaveAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public string? GetPublicUrl(string key) =>
        string.IsNullOrWhiteSpace(settings.PublicBaseUrl)
            ? $"{RoutePrefix}/{key}"
            : settings.BuildPublicUrl(key);

    /// <summary>配信用。キーが不正か、ファイルがなければ null。</summary>
    public string? FindFile(string key)
    {
        if (!ThreadImage.IsValidKey(key))
        {
            return null;
        }

        var path = ResolvePath(key);
        return File.Exists(path) ? path : null;
    }

    private string ResolvePath(string key)
    {
        // キーの形式を厳密に確認しているので、パスの外に出ることはない
        if (!ThreadImage.IsValidKey(key))
        {
            throw new ArgumentException("画像キーの形式が正しくありません。", nameof(key));
        }

        return Path.Combine(_rootDirectory, key.Replace('/', Path.DirectorySeparatorChar));
    }
}
