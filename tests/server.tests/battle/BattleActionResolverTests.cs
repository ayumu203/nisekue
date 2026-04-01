using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using Xunit;

namespace server.tests;

public class BattleActionResolverTests
{
    [Fact]
    public void Resolve_WhenNormalAttackHits_DealsDamageToTarget()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var actorState = CreateState(actor.Id);
        var targetState = CreateState(target.Id);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.NormalAttack, EnemyTarget()),
            [actor, target],
            [actorState, targetState],
            []);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].TargetActorId.Should().Be(target.Id);
        result.TargetResults[0].Damage.Should().Be(5);
        targetState.CurrentHp.Should().Be(25);
    }

    [Fact]
    public void Resolve_WhenUseMoveAndMoveNotLearned_ReturnsFailure()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally, learnedMoveIds: []);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var move = CreateDamageMove(1, mpCost: 1, executionPriority: 0);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, EnemyTarget(), new MoveId(421)),
            [actor, target],
            [CreateState(actor.Id), CreateState(target.Id)],
            [move]);

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be(BattleActionFailureReason.MoveUnavailable);
    }

    [Fact]
    public void Resolve_WhenUseMoveAndMpInsufficient_ReturnsFailure()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var actorState = CreateState(actor.Id, currentMp: 0);
        var move = CreateDamageMove(1, mpCost: 1, executionPriority: 0);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, EnemyTarget(), new MoveId(421)),
            [actor, target],
            [actorState, CreateState(target.Id)],
            [move]);

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be(BattleActionFailureReason.InsufficientMp);
        actorState.CurrentMp.Should().Be(0);
    }

    [Fact]
    public void Resolve_WhenUseMoveSucceeds_DealsDamageAndConsumesMp()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var actorState = CreateState(actor.Id, currentMp: 3);
        var targetState = CreateState(target.Id);
        var move = CreateDamageMove(1, mpCost: 1, executionPriority: 0);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, EnemyTarget(), new MoveId(421)),
            [actor, target],
            [actorState, targetState],
            [move]);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].TargetActorId.Should().Be(target.Id);
        result.TargetResults[0].Damage.Should().Be(6);
        result.TargetResults[0].IsDefeated.Should().BeFalse();
        actorState.CurrentMp.Should().Be(2);
        targetState.CurrentHp.Should().Be(24);
    }

    [Fact]
    public void Resolve_WhenMoveUsesDefenseAsAttackStat_UsesDefenseForDamageCalculation()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var actorState = CreateState(actor.Id, currentMp: 3);
        actorState.ApplyBuff(new BattleBuffState(BuffStat.Defense, BuffCalculationType.Add, 10m, 3), canStack: false);
        var targetState = CreateState(target.Id);
        var move = CreateDefenseDamageMove(1, mpCost: 1, executionPriority: 0);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, EnemyTarget(), new MoveId(421)),
            [actor, target],
            [actorState, targetState],
            [move]);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].Damage.Should().Be(13);
        targetState.CurrentHp.Should().Be(17);
    }

    [Fact]
    public void Resolve_WhenGuard_AppliesDefenseAndIntelligenceBuffs()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var actorState = CreateState(actor.Id);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.Guard, SelfTarget()),
            [actor],
            [actorState],
            []);

        result.Succeeded.Should().BeTrue();
        actorState.Buffs.Should().HaveCount(2);
        actorState.Buffs.Should().Contain(x => x.Stat == BuffStat.Defense && x.CalculationType == BuffCalculationType.Mul && x.Value == 1.5m);
        actorState.Buffs.Should().Contain(x => x.Stat == BuffStat.Intelligence && x.CalculationType == BuffCalculationType.Mul && x.Value == 1.5m);
    }

    [Fact]
    public void Resolve_WhenPrayer_AppliesStackableStrengthBuffToTarget()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(3, BattleSide.Ally, learnedMoveIds: []);
        var actorState = CreateState(actor.Id);
        var targetState = CreateState(target.Id);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.Prayer, new BattleTargetSelector(TargetType.Ally, AttackRange.Single, [target.Id])),
            [actor, target],
            [actorState, targetState],
            []);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].TargetActorId.Should().Be(target.Id);
        result.TargetResults[0].Damage.Should().Be(0);
        targetState.Buffs.Should().ContainSingle(x =>
            x.Stat == BuffStat.Strength &&
            x.CalculationType == BuffCalculationType.Mul &&
            x.Value == 1.5m &&
            x.RemainingTurns == 1);
    }

    [Fact]
    public void Resolve_WhenPrayerIsUsedTwice_StacksStrengthBuffs()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(3, BattleSide.Ally, learnedMoveIds: []);
        var actorState = CreateState(actor.Id);
        var targetState = CreateState(target.Id);
        var action = new BattleAction(actor.Id, BattleActionKind.Prayer, new BattleTargetSelector(TargetType.Ally, AttackRange.Single, [target.Id]));

        resolver.Resolve(action, [actor, target], [actorState, targetState], []);
        resolver.Resolve(action, [actor, target], [actorState, targetState], []);

        targetState.Buffs.Should().HaveCount(2);
        targetState.Buffs.Should().OnlyContain(x =>
            x.Stat == BuffStat.Strength &&
            x.CalculationType == BuffCalculationType.Mul &&
            x.Value == 1.5m &&
            x.RemainingTurns == 1);
    }

    [Fact]
    public void Resolve_WhenWait_ReturnsSuccessWithoutTargetResults()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.Wait, SelfTarget()),
            [actor],
            [CreateState(actor.Id)],
            []);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WhenMoveHasDamageThenHeal_AppliesEffectsInOrderToSameTarget()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var actorState = CreateState(actor.Id, currentMp: 5);
        var targetState = CreateState(target.Id, currentHp: 10);
        var move = CreateDamageThenHealMove(1);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, EnemyTarget(), new MoveId(421)),
            [actor, target],
            [actorState, targetState],
            [move]);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().HaveCount(2);
        result.TargetResults[0].Damage.Should().Be(30);
        result.TargetResults[0].IsDefeated.Should().BeTrue();
        result.TargetResults[1].Damage.Should().Be(0);
        targetState.CurrentHp.Should().Be(15);
        actorState.CurrentMp.Should().Be(4);
    }

    [Fact]
    public void Resolve_WhenMoveRestoresMp_RecoversMpUpToMax()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(3, BattleSide.Ally, learnedMoveIds: []);
        var actorState = CreateState(actor.Id, currentMp: 5);
        var targetState = CreateState(target.Id, currentMp: 2);
        var move = CreateRestoreMpMove(1);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, new BattleTargetSelector(TargetType.Ally, AttackRange.Single, [target.Id]), new MoveId(421)),
            [actor, target],
            [actorState, targetState],
            [move]);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].TargetActorId.Should().Be(target.Id);
        result.TargetResults[0].Damage.Should().Be(0);
        targetState.CurrentMp.Should().Be(10);
        actorState.CurrentMp.Should().Be(4);
    }

    [Fact]
    public void Resolve_WhenActorIsDead_ReturnsFailure()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.NormalAttack, EnemyTarget()),
            [actor, target],
            [CreateState(actor.Id, currentHp: 0), CreateState(target.Id)],
            []);

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be(BattleActionFailureReason.ActorUnavailable);
    }

    [Fact]
    public void Resolve_WhenActorHasParalysis_UsesInjectedRandomProvider()
    {
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var actorState = new BattleActorState(
            actor.Id,
            currentHp: 30,
            currentMp: 10,
            ailments: [new BattleAilmentState(AilmentType.Paralysis, 2)]);
        var targetState = CreateState(target.Id);

        var skippedResolver = CreateResolver(() => 0.2d);
        var skippedResult = skippedResolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.NormalAttack, EnemyTarget()),
            [actor, target],
            [actorState, targetState],
            []);

        skippedResult.Succeeded.Should().BeFalse();
        skippedResult.FailureReason.Should().Be(BattleActionFailureReason.Paralyzed);
        targetState.CurrentHp.Should().Be(30);

        var actingResolver = CreateResolver(() => 0.9d);
        var actingResult = actingResolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.NormalAttack, EnemyTarget()),
            [actor, target],
            [new BattleActorState(
                actor.Id,
                currentHp: 30,
                currentMp: 10,
                ailments: [new BattleAilmentState(AilmentType.Paralysis, 2)]), CreateState(target.Id)],
            []);

        actingResult.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Resolve_WhenNormalAttackHasNoTarget_ReturnsNoTargetFailure()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.NormalAttack, EnemyTarget()),
            [actor],
            [CreateState(actor.Id)],
            []);

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be(BattleActionFailureReason.NoTarget);
    }

    [Fact]
    public void Resolve_WhenTargetHasEvasionBuff_CanMiss()
    {
        var resolver = CreateResolver(() => 0.9d);
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var targetState = CreateState(target.Id);
        targetState.ApplyBuff(new BattleBuffState(BuffStat.Evasion, BuffCalculationType.Add, 40m, 2), canStack: false);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.NormalAttack, EnemyTarget()),
            [actor, target],
            [CreateState(actor.Id), targetState],
            []);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].Damage.Should().Be(0);
        targetState.CurrentHp.Should().Be(30);
    }

    [Fact]
    public void Resolve_WhenActorHasCriticalChanceBuff_ConsumesBuffAfterDamage()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var actorState = CreateState(actor.Id, currentMp: 10);
        actorState.ApplyBuff(new BattleBuffState(BuffStat.CriticalChance, BuffCalculationType.Add, 100m, 2), canStack: false);
        var move = CreateCriticalDamageMove(1);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, EnemyTarget(), new MoveId(421)),
            [actor, target],
            [actorState, CreateState(target.Id)],
            [move]);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].Damage.Should().Be(12);
        actorState.Buffs.Should().NotContain(x => x.Stat == BuffStat.CriticalChance);
    }

    [Fact]
    public void Resolve_WhenTargetHasDamageReductionBuff_ReducesDamage()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var targetState = CreateState(target.Id);
        targetState.ApplyBuff(new BattleBuffState(BuffStat.DamageReduction, BuffCalculationType.Add, 50m, 2), canStack: false);
        var move = CreateDamageMove(1, mpCost: 1, executionPriority: 0);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, EnemyTarget(), new MoveId(421)),
            [actor, target],
            [CreateState(actor.Id), targetState],
            [move]);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].Damage.Should().Be(3);
        targetState.CurrentHp.Should().Be(27);
    }

    [Fact]
    public void TickTurnEnd_WhenRegenerationApplied_RestoresHpUpToStoredMaxHp()
    {
        var state = new BattleActorState(
            new BattleActorId(Guid.NewGuid()),
            currentHp: 20,
            currentMp: 10,
            ailments:
            [
                new BattleAilmentState(
                    AilmentType.Regeneration,
                    2,
                    new DamageEffect(1, 0m, 15, 0m, ElementType.Holy),
                    maxHpLimit: 30)
            ]);

        var results = state.TickTurnEnd();

        results.Should().ContainSingle();
        results[0].HpChange.Should().Be(10);
        state.CurrentHp.Should().Be(30);
    }

    [Fact]
    public void Resolve_WhenInstantDeathTargetsBossAndMoveDisallowsBoss_DoesNotApply()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var move = CreateInstantDeathMove(1, allowBossInstantDeath: false);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, EnemyTarget(), new MoveId(421)),
            [actor, target],
            [CreateState(actor.Id), CreateState(target.Id)],
            [move],
            new BattleFieldContext(bossActorIds: [target.Id]));

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].AppliedAilment.Should().BeNull();
        result.TargetResults[0].IsDefeated.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WhenInstantDeathTargetsBossAndMoveAllowsBoss_Applies()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var target = CreateSnapshot(2, BattleSide.Enemy);
        var targetState = CreateState(target.Id);
        var move = CreateInstantDeathMove(1, allowBossInstantDeath: true);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, EnemyTarget(), new MoveId(421)),
            [actor, target],
            [CreateState(actor.Id), targetState],
            [move],
            new BattleFieldContext(bossActorIds: [target.Id]));

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].AppliedAilment.Should().Be(AilmentType.InstantDeath);
        targetState.IsDead.Should().BeTrue();
    }

    [Fact]
    public void Resolve_WhenAllyHasCoverAll_RedirectsNormalAttackDamageToCoverActor()
    {
        var resolver = CreateResolver();
        var attacker = CreateSnapshot(1, BattleSide.Enemy, learnedMoveIds: []);
        var target = CreateSnapshot(2, BattleSide.Ally, learnedMoveIds: []);
        var cover = CreateSnapshot(3, BattleSide.Ally, learnedMoveIds: []);
        var targetState = CreateState(target.Id);
        var coverState = new BattleActorState(
            cover.Id,
            currentHp: 30,
            currentMp: 10,
            ailments: [new BattleAilmentState(AilmentType.CoverAll, 2)]);

        var result = resolver.Resolve(
            new BattleAction(attacker.Id, BattleActionKind.NormalAttack, new BattleTargetSelector(TargetType.Enemy, AttackRange.Single, [target.Id])),
            [attacker, target, cover],
            [CreateState(attacker.Id), targetState, coverState],
            []);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].TargetActorId.Should().Be(cover.Id);
        targetState.CurrentHp.Should().Be(30);
        coverState.CurrentHp.Should().Be(25);
    }

    [Fact]
    public void Resolve_WhenAllyHasTaunt_RedirectsNormalAttackDamageToTauntingActor()
    {
        var resolver = CreateResolver();
        var attacker = CreateSnapshot(1, BattleSide.Enemy, learnedMoveIds: []);
        var target = CreateSnapshot(2, BattleSide.Ally, learnedMoveIds: []);
        var taunter = CreateSnapshot(3, BattleSide.Ally, learnedMoveIds: []);
        var targetState = CreateState(target.Id);
        var taunterState = new BattleActorState(
            taunter.Id,
            currentHp: 30,
            currentMp: 10,
            ailments: [new BattleAilmentState(AilmentType.Taunt, 1)]);

        var result = resolver.Resolve(
            new BattleAction(attacker.Id, BattleActionKind.NormalAttack, new BattleTargetSelector(TargetType.Enemy, AttackRange.Single, [target.Id])),
            [attacker, target, taunter],
            [CreateState(attacker.Id), targetState, taunterState],
            []);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].TargetActorId.Should().Be(taunter.Id);
        targetState.CurrentHp.Should().Be(30);
        taunterState.CurrentHp.Should().Be(25);
    }

    [Fact]
    public void Resolve_WhenMoveHalvesSelfHp_ReducesCurrentHpToCeilingHalf()
    {
        var resolver = CreateResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var actorState = CreateState(actor.Id, currentHp: 5, currentMp: 10);
        var move = CreateHalveSelfHpMove(1);

        var result = resolver.Resolve(
            new BattleAction(actor.Id, BattleActionKind.UseMove, SelfTarget(), new MoveId(421)),
            [actor],
            [actorState],
            [move]);

        result.Succeeded.Should().BeTrue();
        result.TargetResults.Should().ContainSingle();
        result.TargetResults[0].HpChange.Should().Be(-2);
        actorState.CurrentHp.Should().Be(3);
    }

    private static BattleActionResolver CreateResolver(Func<double>? randomProvider = null)
    {
        return new BattleActionResolver(
            new BattleDamageCalculator(() => 0.99d),
            new BattleStatusResolver(),
            new BattleTargetingResolver(),
            randomProvider);
    }

    private static BattleActorSnapshot CreateSnapshot(int seed, BattleSide side, IReadOnlyList<int>? learnedMoveIds = null)
    {
        var slots = new MoveId?[MoveSet.MaxSlots];
        foreach (var moveId in learnedMoveIds ?? [1])
        {
            var index = Array.FindIndex(slots, x => x is null);
            slots[index] = new MoveId(moveId);
        }

        return new BattleActorSnapshot(
            new BattleActorId(Guid.Parse($"00000000-0000-0000-0000-{seed:D12}")),
            $"Actor{seed}",
            side,
            new Status(maxHp: 30, maxMp: 10, strength: 10, defense: 5, intelligence: 3, luck: 3, speed: 8),
            new MoveSet(slots));
    }

    private static BattleActorState CreateState(BattleActorId actorId, int currentHp = 30, int currentMp = 10)
    {
        return new BattleActorState(actorId, currentHp, currentMp);
    }

    private static BattleTargetSelector EnemyTarget() => new(TargetType.Enemy, AttackRange.Single);

    private static BattleTargetSelector SelfTarget() => new(TargetType.Self, AttackRange.Single);

    private static Move CreateDamageMove(int moveId, int mpCost, int executionPriority)
    {
        return new Move(
            new MoveId(moveId),
            "Damage",
            "damage move",
            TargetType.Enemy,
            AttackRange.Single,
            mpCost,
            executionPriority,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(hitCount: 1, powerRate: 1m, fixedValue: 1, criticalRate: 0m, elementType: ElementType.None))
            ]);
    }

    private static Move CreateHalveSelfHpMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "Halve",
            "halve self hp",
            TargetType.Self,
            AttackRange.Single,
            1,
            1,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.HalveSelfHp)
            ]);
    }

    private static Move CreateDamageThenHealMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "Drain",
            "damage then heal",
            TargetType.Enemy,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(hitCount: 1, powerRate: 0m, fixedValue: 35, criticalRate: 0m, elementType: ElementType.None)),
                new MoveEffect(
                    new MoveEffectId(2),
                    new MoveId(moveId),
                    2,
                    MoveEffectType.Heal,
                    damage: new DamageEffect(hitCount: 1, powerRate: 0m, fixedValue: 15, criticalRate: 0m, elementType: ElementType.Holy))
            ]);
    }

    private static Move CreateDefenseDamageMove(int moveId, int mpCost, int executionPriority)
    {
        return new Move(
            new MoveId(moveId),
            "Shield Bash",
            "damage by defense",
            TargetType.Enemy,
            AttackRange.Single,
            mpCost,
            executionPriority,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(
                        hitCount: 1,
                        powerRate: 1m,
                        fixedValue: 3,
                        criticalRate: 0m,
                        elementType: ElementType.Strike,
                        attackStat: BuffStat.Defense))
            ]);
    }

    private static Move CreateRestoreMpMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "Mana Gift",
            "restore mp",
            TargetType.Ally,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.RestoreMp,
                    damage: new DamageEffect(hitCount: 1, powerRate: 1m, fixedValue: 5, criticalRate: 0m, elementType: ElementType.None, attackStat: BuffStat.Intelligence))
            ]);
    }

    private static Move CreateCriticalDamageMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "Critical",
            "critical move",
            TargetType.Enemy,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(hitCount: 1, powerRate: 1m, fixedValue: 1, criticalRate: 1m, elementType: ElementType.None))
            ]);
    }

    private static Move CreateInstantDeathMove(int moveId, bool allowBossInstantDeath)
    {
        return new Move(
            new MoveId(moveId),
            "Death",
            "instant death",
            TargetType.Enemy,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(AilmentType.InstantDeath, 1m, 1, allowBossInstantDeath: allowBossInstantDeath))
            ]);
    }
}
