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

    private static BattleDamageInput CreateInput(int attackerLuck, int defenderLuck, decimal criticalRate)
    {
        return new BattleDamageInput(
            attackerId: new BattleActorId(Guid.NewGuid()),
            defenderId: new BattleActorId(Guid.NewGuid()),
            attackerStatus: new Status(maxHp: 30, maxMp: 10, strength: 10, defense: 5, intelligence: 3, luck: attackerLuck, speed: 8),
            defenderStatus: new Status(maxHp: 30, maxMp: 10, strength: 8, defense: 5, intelligence: 3, luck: defenderLuck, speed: 6),
            fixedPower: 5,
            powerRate: 1m,
            criticalRate: criticalRate,
            elementType: ElementType.None,
            usesIntelligence: false);
    }
}
