using System.Text;
using FluentAssertions;
using server.domain.chat;
using server.domain.player;
using Xunit;
using DomainThread = server.domain.chat.Thread;

namespace server.tests;

public class ThreadImageTests
{
    [Fact]
    public void Detect_WithPngSignature_ReturnsPng()
    {
        byte[] header = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

        ThreadImageFormatDetector.Detect(header).Should().Be(ThreadImageFormat.Png);
    }

    [Fact]
    public void Detect_WithJpegSignature_ReturnsJpeg()
    {
        byte[] header = [0xFF, 0xD8, 0xFF, 0xE0, 0x00];

        ThreadImageFormatDetector.Detect(header).Should().Be(ThreadImageFormat.Jpeg);
    }

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>")]
    [InlineData("<html><script>alert(1)</script></html>")]
    [InlineData("GIF89a")]
    [InlineData("")]
    public void Detect_WithOtherContent_ReturnsNull(string content)
    {
        ThreadImageFormatDetector.Detect(Encoding.UTF8.GetBytes(content)).Should().BeNull();
    }

    [Theory]
    [InlineData(ThreadImageFormat.Png, ".png")]
    [InlineData(ThreadImageFormat.Jpeg, ".jpg")]
    public void CreateKey_ReturnsValidKeyUnderAuthorDirectory(ThreadImageFormat format, string extension)
    {
        var authorId = new PlayerId(Guid.NewGuid());

        var key = ThreadImage.CreateKey(authorId, format);

        key.Should().StartWith($"threads/{authorId.Value:D}/");
        key.Should().EndWith(extension);
        ThreadImage.IsValidKey(key).Should().BeTrue();
    }

    [Fact]
    public void CreateKey_ReturnsDifferentKeyEveryTime()
    {
        var authorId = new PlayerId(Guid.NewGuid());

        ThreadImage.CreateKey(authorId, ThreadImageFormat.Png)
            .Should().NotBe(ThreadImage.CreateKey(authorId, ThreadImageFormat.Png));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("threads/../../etc/passwd")]
    [InlineData("threads/not-a-guid/0123456789abcdef0123456789abcdef.png")]
    [InlineData("threads/00000000-0000-0000-0000-000000000000/0123456789abcdef0123456789abcdef.svg")]
    [InlineData("other/00000000-0000-0000-0000-000000000000/0123456789abcdef0123456789abcdef.png")]
    [InlineData("threads/00000000-0000-0000-0000-000000000000/../0123456789abcdef0123456789abcdef.png")]
    public void IsValidKey_WithInvalidKey_ReturnsFalse(string? key)
    {
        ThreadImage.IsValidKey(key).Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithInvalidKey_Throws()
    {
        var act = () => new ThreadImage("threads/../secret.png", isSensitive: false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithImage_KeepsImageAndSensitiveFlag()
    {
        var authorId = new PlayerId(Guid.NewGuid());
        var image = new ThreadImage(ThreadImage.CreateKey(authorId, ThreadImageFormat.Jpeg), isSensitive: true);

        var thread = DomainThread.Create(authorId, "タイトル", "本文", image);

        thread.Image.Should().BeSameAs(image);
        thread.Image!.IsSensitive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutImage_HasNoImage()
    {
        var thread = DomainThread.Create(new PlayerId(Guid.NewGuid()), "タイトル", "本文");

        thread.Image.Should().BeNull();
    }
}
