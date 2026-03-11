using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using Xunit;

namespace server.tests;

public class BattleTurnResolverTests
{
    [Fact]
    public void Resolve_WhenFirstActorDefeatsSecondActor_SecondActionFails()
    {
        var resolver = CreateResolver();
        var fastActor = CreateSnapshot(1, speed: 20, strength: 20, side: BattleSide.Ally);
        var slowActor = CreateSnapshot(2, speed: 5, strength: 8, side: BattleSide.Enemy);
        var fastAction = new BattleAction(fastActor.Id, BattleActionKind.NormalAttack, EnemyTarget());
        var slowAction = new BattleAction(slowActor.Id, BattleActionKind.NormalAttack, EnemyTarget());

        var result = resolver.Resolve(
            [fastAction, slowAction],
            [fastActor, slowActor],
            [CreateState(fastActor.Id, currentHp: 30), CreateState(slowActor.Id, currentHp: 10)],
            []);

        result.ActionResults.Should().HaveCount(2);
        result.ActionResults[0].ActorId.Should().Be(fastActor.Id);
        result.ActionResults[0].Succeeded.Should().BeTrue();
        result.ActionResults[1].ActorId.Should().Be(slowActor.Id);
        result.ActionResults[1].Succeeded.Should().BeFalse();
        result.UpdatedStates.Single(x => x.Id == slowActor.Id).CurrentHp.Should().Be(0);
    }

    [Fact]
    public void Resolve_AfterTurnEnd_AppliesPoisonDamageAndExpiresBuff()
    {
        var resolver = CreateResolver();
        var actorId = new BattleActorId(Guid.NewGuid());
        var snapshot = CreateSnapshot(1, speed: 10, strength: 8, actorId: actorId);
        var state = new BattleActorState(
            actorId,
            currentHp: 20,
            currentMp: 8,
            ailments: [new BattleAilmentState(AilmentType.Poison, 1)],
            buffs: [new BattleBuffState(BuffStat.Strength, BuffCalculationType.Mul, 1.5m, 1)]);
        var action = new BattleAction(actorId, BattleActionKind.Wait, SelfTarget());

        var result = resolver.Resolve([action], [snapshot], [state], []);
        var updatedState = result.UpdatedStates.Single();
        var updatedStatus = new BattleStatusResolver().BuildEffectiveStatus(snapshot, updatedState);

        updatedState.CurrentHp.Should().Be(18);
        updatedState.Ailments.Should().BeEmpty();
        updatedState.Buffs.Should().BeEmpty();
        updatedStatus.Strength.Should().Be(8);
    }

    [Fact]
    public void Resolve_AfterTurnEnd_AppliesTrapDamageAndExpiresTrap()
    {
        var resolver = CreateResolver();
        var actorId = new BattleActorId(Guid.NewGuid());
        var snapshot = CreateSnapshot(1, speed: 10, strength: 8, actorId: actorId);
        var state = new BattleActorState(
            actorId,
            currentHp: 20,
            currentMp: 8,
            ailments: [new BattleAilmentState(
                AilmentType.DamageTrap,
                1,
                new DamageEffect(hitCount: 2, powerRate: 0.10m, fixedValue: 0, criticalRate: 0m, elementType: ElementType.None))]);
        var action = new BattleAction(actorId, BattleActionKind.Wait, SelfTarget());

        var result = resolver.Resolve([action], [snapshot], [state], []);
        var updatedState = result.UpdatedStates.Single();

        updatedState.CurrentHp.Should().Be(16);
        updatedState.Ailments.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_AfterTurnEnd_AppliesPoisonTrapDamageAndExpiresTrap()
    {
        var resolver = CreateResolver();
        var actorId = new BattleActorId(Guid.NewGuid());
        var snapshot = CreateSnapshot(1, speed: 10, strength: 8, actorId: actorId);
        var state = new BattleActorState(
            actorId,
            currentHp: 20,
            currentMp: 8,
            ailments: [new BattleAilmentState(AilmentType.PoisonTrap, 1)]);
        var action = new BattleAction(actorId, BattleActionKind.Wait, SelfTarget());

        var result = resolver.Resolve([action], [snapshot], [state], []);
        var updatedState = result.UpdatedStates.Single();

        updatedState.CurrentHp.Should().Be(18);
        updatedState.Ailments.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_AfterTurnEndExpiredMaxHpAndMaxMpBuff_NormalizesCurrentResourcesByRatio()
    {
        var resolver = CreateResolver();

        var actorId = new BattleActorId(Guid.NewGuid());
        var snapshot = CreateSnapshot(1, speed: 7, strength: 8, actorId: actorId);
        var state = new BattleActorState(
            actorId,
            currentHp: 10,
            currentMp: 8,
            buffs:
            [
                new BattleBuffState(BuffStat.MaxHp, BuffCalculationType.Mul, 2.0m, 1),
                new BattleBuffState(BuffStat.MaxMp, BuffCalculationType.Mul, 2.0m, 1)
            ]);
        var action = new BattleAction(
            actorId,
            BattleActionKind.Wait,
            new BattleTargetSelector(TargetType.Self, AttackRange.Single));

        var result = resolver.Resolve([action], [snapshot], [state], []);
        var updatedState = result.UpdatedStates.Should().ContainSingle().Subject;

        updatedState.CurrentHp.Should().Be(5);
        updatedState.CurrentMp.Should().Be(4);
    }

    private static BattleTurnResolver CreateResolver()
    {
        var statusResolver = new BattleStatusResolver();
        var damageCalculator = new BattleDamageCalculator(() => 0.99d);
        var targetingResolver = new BattleTargetingResolver();
        var actionResolver = new BattleActionResolver(damageCalculator, statusResolver, targetingResolver);
        var turnOrderResolver = new BattleTurnOrderResolver(statusResolver);
        return new BattleTurnResolver(turnOrderResolver, actionResolver);
    }

    private static BattleActorSnapshot CreateSnapshot(int seed, int speed, int strength, BattleActorId? actorId = null, BattleSide side = BattleSide.Ally)
    {
        return new BattleActorSnapshot(
            actorId ?? new BattleActorId(Guid.Parse($"10000000-0000-0000-0000-{seed:D12}")),
            $"Tester{seed}",
            side,
            new Status(maxHp: 10, maxMp: 8, strength: strength, defense: 5, intelligence: 4, luck: 3, speed: speed),
            new MoveSet());
    }

    private static BattleActorState CreateState(BattleActorId actorId, int currentHp, int currentMp = 8)
    {
        return new BattleActorState(actorId, currentHp, currentMp);
    }

    private static BattleTargetSelector EnemyTarget() => new(TargetType.Enemy, AttackRange.Single);

    private static BattleTargetSelector SelfTarget() => new(TargetType.Self, AttackRange.Single);
}
