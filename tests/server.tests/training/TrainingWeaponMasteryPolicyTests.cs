using FluentAssertions;
using server.application.training;
using Xunit;

namespace server.tests;

public class TrainingWeaponMasteryPolicyTests
{
    [Theory]
    [InlineData(1, 5)]
    [InlineData(5, 6)]
    [InlineData(10, 7)]
    [InlineData(25, 10)]
    public void ResolveAllowedLevelGap_ReturnsConfiguredFormulaResult(int playerLevel, int expected)
    {
        var policy = new TrainingWeaponMasteryPolicy();

        var result = policy.ResolveAllowedLevelGap(playerLevel);

        result.Should().Be(expected);
    }

    [Fact]
    public void ShouldIncrease_WhenLevelGapIsWithinThreshold_ReturnsTrue()
    {
        var policy = new TrainingWeaponMasteryPolicy();

        policy.ShouldIncrease(playerLevel: 10, enemyLevel: 17).Should().BeTrue();
    }

    [Fact]
    public void ShouldIncrease_WhenLevelGapExceedsThreshold_ReturnsFalse()
    {
        var policy = new TrainingWeaponMasteryPolicy();

        policy.ShouldIncrease(playerLevel: 10, enemyLevel: 18).Should().BeFalse();
    }
}
