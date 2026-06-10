using FluentAssertions;
using server.domain.pet;
using Xunit;

namespace server.tests.pet;

public class PetBonusStatusTests
{
    [Fact]
    public void Constructor_WithDefaults_InitializesAllStatsToZero()
    {
        var bonus = new PetBonusStatus();

        bonus.MaxHp.Should().Be(0);
        bonus.MaxMp.Should().Be(0);
        bonus.Strength.Should().Be(0);
        bonus.Defense.Should().Be(0);
        bonus.Intelligence.Should().Be(0);
        bonus.Luck.Should().Be(0);
        bonus.Speed.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new PetBonusStatus(maxHp: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Add_SumsEachStat()
    {
        var left = new PetBonusStatus(maxHp: 10, maxMp: 9, strength: 8, defense: 7, intelligence: 6, luck: 5, speed: 4);
        var right = new PetBonusStatus(maxHp: 1, maxMp: 2, strength: 3, defense: 4, intelligence: 5, luck: 6, speed: 7);

        var sum = left.Add(right);

        sum.MaxHp.Should().Be(11);
        sum.MaxMp.Should().Be(11);
        sum.Strength.Should().Be(11);
        sum.Defense.Should().Be(11);
        sum.Intelligence.Should().Be(11);
        sum.Luck.Should().Be(11);
        sum.Speed.Should().Be(11);
    }

    [Fact]
    public void Add_WhenOverflow_ClampsToIntMax()
    {
        var left = new PetBonusStatus(maxHp: int.MaxValue);
        var right = new PetBonusStatus(maxHp: 1);

        var sum = left.Add(right);

        sum.MaxHp.Should().Be(int.MaxValue);
    }
}
