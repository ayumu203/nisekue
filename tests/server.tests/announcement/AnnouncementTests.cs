using FluentAssertions;
using server.domain;
using server.domain.announcement;
using Xunit;

namespace server.tests.announcement;

public class AnnouncementTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WhenTitleAndBodyHaveSurroundingSpaces_TrimsThemAndNormalizesLineBreaks()
    {
        var announcement = Announcement.Create(
            new AnnouncementId("2026-11-13-test"),
            new DateOnly(2026, 11, 13),
            "  タイトル  ",
            "\r\n1行目\r\n2行目\r\n",
            Now);

        announcement.Title.Should().Be("タイトル");
        announcement.Body.Should().Be("1行目\n2行目");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenTitleIsBlank_ThrowsDomainException(string title)
    {
        var act = () => Announcement.Create(new AnnouncementId("a"), new DateOnly(2026, 11, 13), title, "本文", Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WhenBodyIsTooLong_ThrowsDomainException()
    {
        var act = () => Announcement.Create(new AnnouncementId("a"), new DateOnly(2026, 11, 13), "タイトル", new string('あ', 2001), Now);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("2026-11-13-maintenance")]
    [InlineData("abc123")]
    public void AnnouncementId_WhenValueIsValid_KeepsValue(string value)
    {
        new AnnouncementId(value).Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Upper")]
    [InlineData("with space")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("double--hyphen")]
    [InlineData("日本語")]
    public void AnnouncementId_WhenValueIsInvalid_ThrowsDomainException(string value)
    {
        var act = () => new AnnouncementId(value);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Generate_WhenCalled_StartsWithDateAndIsValid()
    {
        var id = AnnouncementId.Generate(new DateOnly(2026, 11, 13));

        id.Value.Should().MatchRegex("^2026-11-13-[0-9a-f]{8}$");
    }
}
