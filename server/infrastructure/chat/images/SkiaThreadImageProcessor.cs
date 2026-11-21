using SkiaSharp;
using server.domain.chat;
using server.shared.constants.chat;

namespace server.infrastructure.chat.images;

/// <summary>
/// SkiaSharp で画像を一度デコードしてから再エンコードする。
/// - png / jpeg 以外（SVG など）や、画像として読めないものは拒否する
/// - 長辺を ThreadConstants.ImageMaxLongEdge まで縮小する
/// - EXIF の向きを反映したうえで、EXIF などのメタデータは捨てる（再エンコードで消える）
/// - 形式は元のまま（png は png、jpeg は jpeg）
/// </summary>
public sealed class SkiaThreadImageProcessor : IThreadImageProcessor
{
    public ProcessedThreadImage Process(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length == 0)
        {
            throw new ArgumentException("画像ファイルが空です。", nameof(content));
        }

        if (content.Length > ThreadConstants.ImageMaxUploadBytes)
        {
            throw new ArgumentException(
                $"画像は{ThreadConstants.ImageMaxUploadBytes / 1024 / 1024}MBまでです。",
                nameof(content));
        }

        var format = ThreadImageFormatDetector.Detect(content)
            ?? throw new ArgumentException("画像は png または jpeg のみ投稿できます。", nameof(content));

        using var data = SKData.CreateCopy(content);
        using var codec = SKCodec.Create(data)
            ?? throw new ArgumentException("画像を読み込めませんでした。", nameof(content));

        var encodedFormat = ToSkiaFormat(format);
        if (codec.EncodedFormat != encodedFormat)
        {
            throw new ArgumentException("画像の形式が正しくありません。", nameof(content));
        }

        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || (long)info.Width * info.Height > ThreadConstants.ImageMaxDecodedPixels)
        {
            throw new ArgumentException("画像の縦横のサイズが大きすぎます。", nameof(content));
        }

        using var decoded = SKBitmap.Decode(codec)
            ?? throw new ArgumentException("画像を読み込めませんでした。", nameof(content));

        using var oriented = ApplyOrigin(decoded, codec.EncodedOrigin);
        using var resized = ResizeToFit(oriented ?? decoded, ThreadConstants.ImageMaxLongEdge);
        var output = resized ?? oriented ?? decoded;

        using var encoded = output.Encode(encodedFormat, ThreadConstants.ImageJpegQuality)
            ?? throw new InvalidOperationException("画像の変換に失敗しました。");

        return new ProcessedThreadImage(encoded.ToArray(), format, output.Width, output.Height);
    }

    private static SKEncodedImageFormat ToSkiaFormat(ThreadImageFormat format) => format switch
    {
        ThreadImageFormat.Png => SKEncodedImageFormat.Png,
        ThreadImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    /// <summary>長辺が maxLongEdge を超えるときだけ縮小した画像を返す。縮小不要なら null。</summary>
    private static SKBitmap? ResizeToFit(SKBitmap source, int maxLongEdge)
    {
        var longEdge = Math.Max(source.Width, source.Height);
        if (longEdge <= maxLongEdge)
        {
            return null;
        }

        var scale = (double)maxLongEdge / longEdge;
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        return source.Resize(source.Info.WithSize(width, height), sampling)
            ?? throw new InvalidOperationException("画像の縮小に失敗しました。");
    }

    /// <summary>
    /// EXIF の向き（スマホの写真など）を画素に反映した画像を返す。向きの補正が不要なら null。
    /// 再エンコードで EXIF は消えるので、先に向きを反映しておかないと横倒しで表示される。
    /// </summary>
    private static SKBitmap? ApplyOrigin(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return null;
        }

        float w = source.Width;
        float h = source.Height;
        var swapsAxes = origin is SKEncodedOrigin.LeftTop
            or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom
            or SKEncodedOrigin.LeftBottom;

        // 元画像の座標 (x, y) を、表示上の座標 (x', y') に写す行列
        // x' = ScaleX * x + SkewX * y + TransX, y' = SkewY * x + ScaleY * y + TransY
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => CreateMatrix(-1, 0, w, 0, 1, 0),
            SKEncodedOrigin.BottomRight => CreateMatrix(-1, 0, w, 0, -1, h),
            SKEncodedOrigin.BottomLeft => CreateMatrix(1, 0, 0, 0, -1, h),
            SKEncodedOrigin.LeftTop => CreateMatrix(0, 1, 0, 1, 0, 0),
            SKEncodedOrigin.RightTop => CreateMatrix(0, -1, h, 1, 0, 0),
            SKEncodedOrigin.RightBottom => CreateMatrix(0, -1, h, -1, 0, w),
            SKEncodedOrigin.LeftBottom => CreateMatrix(0, 1, 0, -1, 0, w),
            _ => SKMatrix.Identity
        };

        var info = swapsAxes
            ? source.Info.WithSize(source.Height, source.Width)
            : source.Info;
        var result = new SKBitmap(info);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);
        canvas.SetMatrix(matrix);
        canvas.DrawBitmap(source, 0, 0);
        canvas.Flush();
        return result;
    }

    private static SKMatrix CreateMatrix(float scaleX, float skewX, float transX, float skewY, float scaleY, float transY) =>
        new()
        {
            ScaleX = scaleX,
            SkewX = skewX,
            TransX = transX,
            SkewY = skewY,
            ScaleY = scaleY,
            TransY = transY,
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1
        };
}
