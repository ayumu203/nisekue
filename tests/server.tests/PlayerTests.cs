using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerTests
{
    // レベルが0ではプレイヤーの初期化でエラーが出る.
    [Fact]
    public void Constructor_WhenLevelIsZero_ThrowsArgumentOutOfRangeException()
    {
        var act = () => CreatePlayer(level: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
    // レベルが0未満ではプレイヤーの初期化でエラーが出る.
    [Fact]
    public void Constructor_WhenLevelIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var act = () => CreatePlayer(level: -5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
    // レベルが1以上ならok
    [Fact]
    public void Constructor_WhenLevelIsPositive_KeepsOriginalValue()
    {
        var player = CreatePlayer(level: 1);

        player.Level.Should().Be(1);
    }
    // レベルが1以上ならok2
    [Fact]
    public void Constructor_WhenLevelIsPositive_KeepsOriginalValue2()
    {
        var player = CreatePlayer(level: 5);

        player.Level.Should().Be(5);
    }

    private static Player CreatePlayer(int level) =>
        new(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: level,
            exp: 0,
            status: new Status(
                maxHp: 10,
                maxMp: 0,
                strength: 1,
                defense: 1,
                intelligence: 1,
                luck: 1,
                speed: 1));
}
