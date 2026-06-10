using FluentAssertions;
using server.domain.pet;
using Xunit;

namespace server.tests.pet;

public class PetCaptureRateCalculatorTests
{
    [Theory]
    [InlineData(1, 50)]
    [InlineData(19, 50)]
    [InlineData(20, 40)]
    [InlineData(39, 40)]
    [InlineData(40, 30)]
    [InlineData(59, 30)]
    [InlineData(60, 20)]
    [InlineData(99, 20)]
    public void GetBaseRatePercent_WithEnemyLevelTier_ReturnsTierRate(int enemyLevel, int expected)
    {
        PetCaptureRateCalculator.GetBaseRatePercent(enemyLevel).Should().Be(expected);
    }

    [Fact]
    public void Calculate_WithSameLevelLowTierEnemy_ReturnsBaseRate()
    {
        PetCaptureRateCalculator.Calculate(playerLevel: 10, enemyLevel: 10).Should().Be(50);
    }

    [Fact]
    public void Calculate_WithLevelAdvantage_AddsFivePercentPerLevel()
    {
        PetCaptureRateCalculator.Calculate(playerLevel: 15, enemyLevel: 10).Should().Be(75);
    }

    [Fact]
    public void Calculate_WithLevelDisadvantage_SubtractsFivePercentPerLevel()
    {
        PetCaptureRateCalculator.Calculate(playerLevel: 5, enemyLevel: 10).Should().Be(25);
    }

    [Fact]
    public void Calculate_WithHighTierEnemyAndLevelDiff_UsesTierBaseRate()
    {
        // 基礎率20%（敵Lv60+）+ Lv差+10×5% = 70%
        PetCaptureRateCalculator.Calculate(playerLevel: 70, enemyLevel: 60).Should().Be(70);
    }

    [Fact]
    public void Calculate_WhenRateBelowMinimum_ClampsToMinimum()
    {
        // 50 + (1-19)×5 = -40 → 下限5%
        PetCaptureRateCalculator.Calculate(playerLevel: 1, enemyLevel: 19).Should().Be(PetCaptureRateCalculator.MinRatePercent);
    }

    [Fact]
    public void Calculate_WhenRateAboveMaximum_ClampsToMaximum()
    {
        // 50 + (50-1)×5 = 295 → 上限95%
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
