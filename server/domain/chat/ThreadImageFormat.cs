namespace server.domain.chat;

/// <summary>スレッドに投稿できる画像の形式。SVG などは受け付けない。</summary>
public enum ThreadImageFormat
{
    Png,
    Jpeg
}

public static class ThreadImageFormatExtensions
{
    public static string ToExtension(this ThreadImageFormat format) => format switch
    {
        ThreadImageFormat.Png => "png",
        ThreadImageFormat.Jpeg => "jpg",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    public static string ToContentType(this ThreadImageFormat format) => format switch
    {
        ThreadImageFormat.Png => "image/png",
        ThreadImageFormat.Jpeg => "image/jpeg",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };
}
