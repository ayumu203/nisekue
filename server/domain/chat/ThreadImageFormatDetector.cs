namespace server.domain.chat;

/// <summary>
/// ファイルの先頭のバイト列（マジックナンバー）から画像の形式を判定する。
/// 拡張子や Content-Type は信用せず、中身で判定する。
/// </summary>
public static class ThreadImageFormatDetector
{
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];

    /// <summary>png / jpeg なら形式を、それ以外は null を返す。</summary>
    public static ThreadImageFormat? Detect(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(PngSignature))
        {
            return ThreadImageFormat.Png;
        }

        if (header.StartsWith(JpegSignature))
        {
            return ThreadImageFormat.Jpeg;
        }

        return null;
    }
}
