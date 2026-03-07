using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerTests
{
    [Fact]
    public void UpdateName_WithTrimmedValidName_UpdatesName()
    {
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: 1,
            exp: 0,
            status: new BaseStatus(maxHp: 10, maxMp: 0, strength: 1, defense: 1, intelligence: 1, luck: 1, speed: 1));

        player.UpdateName("  Renamed Player  ");

        player.Name.Should().Be("Renamed Player");
    }

    [Fact]
    public void UpdateName_WithTooLongName_ThrowsArgumentException()
    {
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: 1,
            exp: 0,
            status: new BaseStatus(maxHp: 10, maxMp: 0, strength: 1, defense: 1, intelligence: 1, luck: 1, speed: 1));

        var act = () => player.UpdateName(new string('a', 21));

        act.Should().Throw<ArgumentException>().WithMessage("プレイヤー名は1文字から20文字以内です.*");
    }
}
