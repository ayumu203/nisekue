using FluentAssertions;
using server.domain.battle;
using server.domain.move.enums;
using server.domain.player;
using Xunit;

namespace server.tests;

public class BattleDamageCalculatorTests
{
    [Fact]
    public void Calculate_WhenLuckDisadvantage_UsesMinimumCriticalChance()
    {
        var calculator = new BattleDamageCalculator(() => 0.049d);

        var result = calculator.Calculate(CreateInput(attackerLuck: 5, defenderLuck: 10, criticalRate: 0.5m));

        result.IsCritical.Should().BeTrue();
    }

    [Fact]
    public void Calculate_WhenLuckAdvantageReachesCap_UsesMaximumCriticalChance()
    {
        var calculator = new BattleDamageCalculator(() => 0.299d);

        var result = calculator.Calculate(CreateInput(attackerLuck: 30, defenderLuck: 5, criticalRate: 0.5m));

        result.IsCritical.Should().BeTrue();
    }

    [Fact]
    public void Calculate_WhenRollExceedsCriticalChance_DoesNotCriticallyHit()
    {
        var calculator = new BattleDamageCalculator(() => 0.30d);

        var result = calculator.Calculate(CreateInput(attackerLuck: 30, defenderLuck: 5, criticalRate: 0.5m));

        result.IsCritical.Should().BeFalse();
    }

    [Fact]
    public void Calculate_WhenCriticalRateIsZero_DoesNotCriticallyHit()
    {
        var calculator = new BattleDamageCalculator(() => 0.0d);

        var result = calculator.Calculate(CreateInput(attackerLuck: 30, defenderLuck: 5, criticalRate: 0m));

        result.IsCritical.Should().BeFalse();
    }

    [Fact]
    public void Calculate_WhenAttackStatIsIntelligence_UsesIntelligenceAndTargetIntelligenceForDamage()
    {
        var calculator = new BattleDamageCalculator(() => 0.99d);

        var result = calculator.Calculate(CreateInput(
            attackerLuck: 10,
            defenderLuck: 10,
            criticalRate: 0m,
            attackerStrength: 4,
            attackerIntelligence: 20,
            defenderDefense: 10,
            defenderIntelligence: 3,
            attackStat: BuffStat.Intelligence));

        result.Damage.Should().Be(22);
        result.IsCritical.Should().BeFalse();
    }

    [Fact]
    public void Calculate_WhenAttackStatIsStrength_UsesStrengthAndTargetDefenseForDamage()
    {
        var calculator = new BattleDamageCalculator(() => 0.99d);

        var result = calculator.Calculate(CreateInput(
            attackerLuck: 10,
            defenderLuck: 10,
            criticalRate: 0m,
            attackerStrength: 20,
            attackerIntelligence: 4,
            defenderDefense: 3,
            defenderIntelligence: 10,
            attackStat: BuffStat.Strength));

        result.Damage.Should().Be(22);
        result.IsCritical.Should().BeFalse();
    }

    [Fact]
    public void Calculate_WhenCriticalHit_AppliesCriticalMultiplierToDamage()
    {
        var calculator = new BattleDamageCalculator(() => 0.0d);

        var result = calculator.Calculate(CreateInput(
            attackerLuck: 30,
            defenderLuck: 5,
            criticalRate: 0.5m,
            attackerStrength: 10,
            attackerIntelligence: 3,
            defenderDefense: 5,
            defenderIntelligence: 3,
            attackStat: BuffStat.Strength));

        result.Damage.Should().Be(15);
        result.IsCritical.Should().BeTrue();
    }

    [Fact]
    public void Calculate_WhenCriticalHitAndAttackStatIsIntelligence_AppliesCriticalMultiplierToIntelligenceDamage()
    {
        var calculator = new BattleDamageCalculator(() => 0.0d);

        var result = calculator.Calculate(CreateInput(
            attackerLuck: 30,
            defenderLuck: 5,
            criticalRate: 0.5m,
            attackerStrength: 4,
            attackerIntelligence: 20,
            defenderDefense: 10,
            defenderIntelligence: 3,
            attackStat: BuffStat.Intelligence));

        result.Damage.Should().Be(33);
        result.IsCritical.Should().BeTrue();
    }

    [Fact]
    public void Calculate_WhenAttackStatIsIntelligence_ReducesDamageByAttackDefenseRatio()
    {
        var calculator = new BattleDamageCalculator(() => 0.99d);

        var result = calculator.Calculate(CreateInput(
            attackerLuck: 10,
            defenderLuck: 10,
            criticalRate: 0m,
            attackerStrength: 4,
            attackerIntelligence: 12,
            defenderDefense: 10,
            defenderIntelligence: 12,
            attackStat: BuffStat.Intelligence));

        result.Damage.Should().Be(9);
        result.IsCritical.Should().BeFalse();
    }

    [Fact]
    public void Calculate_WhenAttackStatIsLuck_UsesLuckAsAttackPower()
    {
        var calculator = new BattleDamageCalculator(() => 0.99d);

        var result = calculator.Calculate(CreateInput(
            attackerLuck: 18,
            defenderLuck: 10,
            criticalRate: 0m,
            attackerStrength: 4,
            defenderDefense: 5,
            attackStat: BuffStat.Luck));

        result.Damage.Should().Be(18);
        result.IsCritical.Should().BeFalse();
    }

    [Fact]
    public void Calculate_WhenIntelligenceAttackAndBothIntelligenceAreZero_ReturnsMinimumDamage()
    {
        var calculator = new BattleDamageCalculator(() => 0.99d);

        var result = calculator.Calculate(CreateInput(
            attackerLuck: 10,
            defenderLuck: 10,
            criticalRate: 0m,
            attackerStrength: 4,
            attackerIntelligence: 0,
            defenderDefense: 10,
            defenderIntelligence: 0,
            attackStat: BuffStat.Intelligence));

        result.Damage.Should().Be(1);
        result.IsCritical.Should().BeFalse();
    }

    private static BattleDamageInput CreateInput(
        int attackerLuck,
        int defenderLuck,
        decimal criticalRate,
        int attackerStrength = 10,
        int attackerIntelligence = 3,
        int defenderDefense = 5,
        int defenderIntelligence = 3,
        BuffStat attackStat = BuffStat.Strength)
    {
        return new BattleDamageInput(
            attackerId: new BattleActorId(Guid.NewGuid()),
            defenderId: new BattleActorId(Guid.NewGuid()),
            attackerStatus: new Status(maxHp: 30, maxMp: 10, strength: attackerStrength, defense: 5, intelligence: attackerIntelligence, luck: attackerLuck, speed: 8),
            defenderStatus: new Status(maxHp: 30, maxMp: 10, strength: 8, defense: defenderDefense, intelligence: defenderIntelligence, luck: defenderLuck, speed: 6),
            fixedPower: 5,
            powerRate: 1m,
            criticalRate: criticalRate,
            criticalChanceBonus: 0m,
            elementType: ElementType.None,
            attackStat: attackStat);
    }
}
