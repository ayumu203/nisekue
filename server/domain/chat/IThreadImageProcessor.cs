namespace server.domain.chat;

/// <summary>
/// アップロードされた画像を検証し、再エンコードする。
/// 画像として読めないもの、png / jpeg 以外のものは ArgumentException を投げる。
/// </summary>
public interface IThreadImageProcessor
{
    ProcessedThreadImage Process(byte[] content);
}

/// <summary>再エンコード後の画像。</summary>
public sealed record ProcessedThreadImage(byte[] Content, ThreadImageFormat Format, int Width, int Height);
