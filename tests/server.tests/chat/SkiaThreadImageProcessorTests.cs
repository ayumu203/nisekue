using System.Text;
using FluentAssertions;
using server.domain.chat;
using server.infrastructure.chat.images;
using server.shared.constants.chat;
using SkiaSharp;
using Xunit;

namespace server.tests;

public class SkiaThreadImageProcessorTests
{
    private readonly SkiaThreadImageProcessor _processor = new();

    [Fact]
    public void Process_WithSmallPng_KeepsSizeAndFormat()
    {
        var content = CreateImage(200, 100, SKEncodedImageFormat.Png);

        var result = _processor.Process(content);

        result.Format.Should().Be(ThreadImageFormat.Png);
        result.Width.Should().Be(200);
        result.Height.Should().Be(100);
        ThreadImageFormatDetector.Detect(result.Content).Should().Be(ThreadImageFormat.Png);
    }

    [Fact]
    public void Process_WithLargeJpeg_ResizesLongEdgeAndKeepsJpeg()
    {
        var content = CreateImage(2560, 1440, SKEncodedImageFormat.Jpeg);

        var result = _processor.Process(content);

        result.Format.Should().Be(ThreadImageFormat.Jpeg);
        result.Width.Should().Be(ThreadConstants.ImageMaxLongEdge);
        result.Height.Should().Be(720);
        ThreadImageFormatDetector.Detect(result.Content).Should().Be(ThreadImageFormat.Jpeg);
    }

    [Fact]
    public void Process_WithTallPng_ResizesByHeight()
    {
        var content = CreateImage(640, 2560, SKEncodedImageFormat.Png);

        var result = _processor.Process(content);

        result.Width.Should().Be(320);
        result.Height.Should().Be(ThreadConstants.ImageMaxLongEdge);
    }

    [Fact]
    public void Process_WithSvg_Throws()
    {
        var content = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");

        var act = () => _processor.Process(content);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Process_WithPngSignatureButBrokenBody_Throws()
    {
        byte[] content = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. Encoding.UTF8.GetBytes("<html>not a png</html>")];

        var act = () => _processor.Process(content);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Process_WithEmptyContent_Throws()
    {
        var act = () => _processor.Process([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Process_WithTooLargeFile_Throws()
    {
        var content = new byte[ThreadConstants.ImageMaxUploadBytes + 1];
        content[0] = 0xFF;
        content[1] = 0xD8;
        content[2] = 0xFF;

        var act = () => _processor.Process(content);

        act.Should().Throw<ArgumentException>();
    }

    private static byte[] CreateImage(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.SkyBlue);
            using var paint = new SKPaint { Color = SKColors.OrangeRed };
            canvas.DrawRect(0, 0, width / 2f, height / 2f, paint);
        }

        using var data = bitmap.Encode(format, 90);
        return data.ToArray();
    }
}
