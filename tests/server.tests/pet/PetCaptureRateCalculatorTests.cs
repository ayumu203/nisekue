using FluentAssertions;
using server.domain.pet;
using Xunit;

namespace server.tests.pet;

public class PetCaptureRateCalculatorTests
{
    [Theory]
    [InlineData(1, 75)]
    [InlineData(9, 75)]
    [InlineData(10, 60)]
    [InlineData(19, 60)]
    [InlineData(20, 50)]
    [InlineData(39, 50)]
    [InlineData(40, 40)]
    [InlineData(59, 40)]
    [InlineData(60, 30)]
    [InlineData(99, 30)]
    [InlineData(100, 20)]
    [InlineData(199, 20)]
    [InlineData(200, 15)]
    [InlineData(399, 15)]
    [InlineData(400, 10)]
    [InlineData(699, 10)]
    [InlineData(700, 5)]
    [InlineData(1000, 5)]
    public void GetBaseRatePercent_WithEnemyLevelTier_ReturnsTierRate(int enemyLevel, int expected)
    {
        PetCaptureRateCalculator.GetBaseRatePercent(enemyLevel).Should().Be(expected);
    }

    [Fact]
    public void Calculate_WithSameLevelEnemy_ReturnsBaseRate()
    {
        PetCaptureRateCalculator.Calculate(playerLevel: 25, enemyLevel: 25).Should().Be(50);
    }

    [Fact]
    public void Calculate_WithLevelAdvantage_AddsFivePercentPerLevel()
    {
        PetCaptureRateCalculator.Calculate(playerLevel: 30, enemyLevel: 25).Should().Be(75);
    }

    [Fact]
    public void Calculate_WithLevelDisadvantage_SubtractsFivePercentPerLevel()
    {
        PetCaptureRateCalculator.Calculate(playerLevel: 20, enemyLevel: 25).Should().Be(25);
    }

    [Fact]
    public void Calculate_WithHighTierEnemyAndLevelDiff_UsesTierBaseRate()
    {
        // 基礎率5%（敵Lv700+）+ Lv差+10×5% = 55%
        PetCaptureRateCalculator.Calculate(playerLevel: 710, enemyLevel: 700).Should().Be(55);
    }

    [Fact]
    public void Calculate_WhenRateBelowMinimum_ClampsToMinimum()
    {
        // 50 + (1-39)×5 = -140 → 下限5%
        PetCaptureRateCalculator.Calculate(playerLevel: 1, enemyLevel: 39).Should().Be(PetCaptureRateCalculator.MinRatePercent);
    }

    [Fact]
    public void Calculate_WhenRateAboveMaximum_ClampsToMaximum()
    {
        // 75 + (50-1)×5 = 320 → 上限95%
        PetCaptureRateCalculator.Calculate(playerLevel: 50, enemyLevel: 1).Should().Be(PetCaptureRateCalculator.MaxRatePercent);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void Calculate_WithNonPositiveLevel_ThrowsArgumentOutOfRangeException(int playerLevel, int enemyLevel)
    {
        var act = () => PetCaptureRateCalculator.Calculate(playerLevel, enemyLevel);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
