using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move.enums;
using server.domain.player;
using Xunit;

namespace server.tests;

public class BattleStatusResolverTests
{
    [Fact]
    public void BuildEffectiveStatus_WhenMaxHpBuffWouldDropBelowOne_ClampsMaxHpToOne()
    {
        var resolver = new BattleStatusResolver();
        var snapshot = CreateSnapshot(maxHp: 10, maxMp: 5, strength: 8);
        var state = CreateState(snapshot.Id, new BattleBuffState(BuffStat.MaxHp, BuffCalculationType.Add, -20m, 1));

        var result = resolver.BuildEffectiveStatus(snapshot, state);

        result.MaxHp.Should().Be(1);
    }

    [Fact]
    public void BuildEffectiveStatus_WhenMaxMpBuffApplied_AddsToMaxMp()
    {
        var resolver = new BattleStatusResolver();
        var snapshot = CreateSnapshot(maxHp: 10, maxMp: 5, strength: 8);
        var state = CreateState(snapshot.Id, new BattleBuffState(BuffStat.MaxMp, BuffCalculationType.Add, 4m, 1));

        var result = resolver.BuildEffectiveStatus(snapshot, state);

        result.MaxMp.Should().Be(9);
    }

    [Fact]
    public void BuildEffectiveStatus_WhenStrengthBuffApplied_MultipliesStrength()
    {
        var resolver = new BattleStatusResolver();
        var snapshot = CreateSnapshot(maxHp: 10, maxMp: 5, strength: 8);
        var state = CreateState(snapshot.Id, new BattleBuffState(BuffStat.Strength, BuffCalculationType.Mul, 1.5m, 1));

        var result = resolver.BuildEffectiveStatus(snapshot, state);

        result.Strength.Should().Be(12);
    }

    [Fact]
    public void BuildEffectiveStatus_WhenStrengthBuffAppliedWithDecimalMultiplier_RoundsAwayFromZero()
    {
        var resolver = new BattleStatusResolver();
        var snapshot = CreateSnapshot(maxHp: 10, maxMp: 5, strength: 9);
        var state = CreateState(snapshot.Id, new BattleBuffState(BuffStat.Strength, BuffCalculationType.Mul, 0.5m, 1));

        var result = resolver.BuildEffectiveStatus(snapshot, state);

        result.Strength.Should().Be(5);
    }

    [Fact]
    public void BuildEffectiveStatus_AfterTurnEndExpiredBuff_ReturnsToBaseStrength()
    {
        var resolver = new BattleStatusResolver();
        var snapshot = CreateSnapshot(maxHp: 10, maxMp: 5, strength: 8);
        var state = CreateState(snapshot.Id, new BattleBuffState(BuffStat.Strength, BuffCalculationType.Mul, 1.5m, 1));

        var beforeTurnEnd = resolver.BuildEffectiveStatus(snapshot, state);
        state.TickTurnEnd();
        var afterTurnEnd = resolver.BuildEffectiveStatus(snapshot, state);

        beforeTurnEnd.Strength.Should().Be(12);
        afterTurnEnd.Strength.Should().Be(8);
    }

    [Fact]
    public void BuildEffectiveStatus_WhenSetBuffApplied_UsesExactValue()
    {
        var resolver = new BattleStatusResolver();
        var snapshot = CreateSnapshot(maxHp: 10, maxMp: 5, strength: 8);
        var state = CreateState(snapshot.Id, new BattleBuffState(BuffStat.Strength, BuffCalculationType.Set, 0m, 2));

        var result = resolver.BuildEffectiveStatus(snapshot, state);

        result.Strength.Should().Be(0);
    }

    private static BattleActorSnapshot CreateSnapshot(int maxHp, int maxMp, int strength)
    {
        return new BattleActorSnapshot(
            new BattleActorId(Guid.NewGuid()),
            "Tester",
            BattleSide.Ally,
            new Status(maxHp: maxHp, maxMp: maxMp, strength: strength, defense: 6, intelligence: 4, luck: 3, speed: 7),
            new MoveSet());
    }

    private static BattleActorState CreateState(BattleActorId actorId, params BattleBuffState[] buffs)
    {
        return new BattleActorState(actorId, currentHp: 10, currentMp: 5, buffs: buffs);
    }
}
