using FluentAssertions;
using server.domain.quest;
using Xunit;

namespace server.tests.quest;

public class QuestEndlessConfigTests
{
    [Theory]
    [InlineData(1, 1.02)]   // 1 + 0.02 * 1^2
    [InlineData(10, 3.0)]   // 1 + 0.02 * 100
    [InlineData(50, 51.0)]  // 1 + 0.02 * 2500
    public void GrowthFactor_FollowsPolynomialModel(int floorNo, double expected)
    {
        CreateConfig().GrowthFactor(floorNo).Should().BeApproximately(expected, 1e-9);
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(10, true)]
    [InlineData(20, true)]
    [InlineData(25, false)]
    public void IsBossFloor_IsMultipleOfBossInterval(int floorNo, bool expected)
    {
        CreateConfig().IsBossFloor(floorNo).Should().Be(expected);
    }

    [Fact]
    public void Constructor_WhenExponentNotGreaterThanOne_Throws()
    {
        var act = () => CreateConfig(p: 1.0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WhenMaxEnemiesLessThanMin_Throws()
    {
        var act = () => CreateConfig(minEnemies: 3, maxEnemies: 1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static QuestEndlessConfig CreateConfig(
        double a = 0.02,
        double p = 2.0,
        int minEnemies = 1,
        int maxEnemies = 3)
        => new(a, p, 7161, 10, 1.5m, 3.0m, 5, 10, minEnemies, maxEnemies, 10000m, 7500m);
}
