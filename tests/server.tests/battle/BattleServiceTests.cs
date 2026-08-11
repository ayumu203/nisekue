using FluentAssertions;
using server.application.battle;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using Xunit;

namespace server.tests;

public class BattleServiceTests
{
    [Fact]
    public void ResolveTurn_WhenNormalAttackIsRepeatedForThreeTurns_UpdatesStateAcrossTurns()
    {
        var service = new BattleService();
        var ally = CreateActorInput(1, BattleSide.Ally, currentHp: 30, strength: 10, speed: 10);
        var enemy = CreateActorInput(2, BattleSide.Enemy, currentHp: 30, strength: 5, speed: 5);
        var actors = new[] { ally, enemy };

        for (var turn = 0; turn < 3; turn++)
        {
            var resolution = service.ResolveTurn(new BattleTurnRequest(
                actors,
                [CreateNormalAttackAction(ally.ActorId)],
                []));

            actors = CarryForwardActors(actors, resolution.UpdatedStates);
        }

        actors.Single(x => x.ActorId == enemy.ActorId).CurrentHp.Should().Be(15);
        actors.Single(x => x.ActorId == ally.ActorId).CurrentHp.Should().Be(30);
    }

    [Fact]
    public void ResolveTurn_WhenNormalAttackDefeatsTargetBeforeItsAction_TargetActionFails()
    {
        var service = new BattleService();
        var ally = CreateActorInput(1, BattleSide.Ally, currentHp: 30, strength: 20, speed: 20);
        var enemy = CreateActorInput(2, BattleSide.Enemy, currentHp: 10, strength: 8, speed: 5);

        var resolution = service.ResolveTurn(new BattleTurnRequest(
            [ally, enemy],
            [CreateNormalAttackAction(ally.ActorId), CreateNormalAttackAction(enemy.ActorId)],
            []));

        resolution.ActionResults.Should().HaveCount(2);
        resolution.ActionResults[0].ActorId.Value.Should().Be(ally.ActorId);
        resolution.ActionResults[0].Succeeded.Should().BeTrue();
        resolution.ActionResults[1].ActorId.Value.Should().Be(enemy.ActorId);
        resolution.ActionResults[1].Succeeded.Should().BeFalse();
        resolution.UpdatedStates.Single(x => x.Id.Value == enemy.ActorId).CurrentHp.Should().Be(0);
    }

    [Fact]
    public void ResolveTurn_WhenRangeAttackHitsMultipleTargets_DefeatsAllTargets()
    {
        var service = new BattleService();
        const int moveId = 100;
        var wideSlash = CreateWideAttackMove(moveId);
        var ally = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], strength: 10, speed: 10);
        var enemy1 = CreateActorInput(2, BattleSide.Enemy, currentHp: 10, strength: 5, speed: 5);
        var enemy2 = CreateActorInput(3, BattleSide.Enemy, currentHp: 10, strength: 5, speed: 4);

        var resolution = service.ResolveTurn(new BattleTurnRequest(
            [ally, enemy1, enemy2],
            [CreateMoveAction(ally.ActorId, moveId, TargetType.Enemy, AttackRange.All)],
            [wideSlash]));

        resolution.ActionResults.Should().ContainSingle();
        resolution.ActionResults[0].TargetResults.Should().HaveCount(2);
        resolution.UpdatedStates.Where(x => x.Id.Value == enemy1.ActorId || x.Id.Value == enemy2.ActorId)
            .Should().OnlyContain(x => x.CurrentHp == 0);
    }

    [Fact]
    public void ResolveTurn_WhenHealMoveIsUsed_RestoresHpAndConsumesMp()
    {
        var service = new BattleService();
        const int moveId = 103;
        var healMove = CreateHealMove(moveId);
        var healer = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], intelligence: 12, speed: 10);
        var ally = CreateActorInput(2, BattleSide.Ally, currentHp: 8, currentMp: 10, speed: 5);

        var resolution = service.ResolveTurn(new BattleTurnRequest(
            [healer, ally],
            [CreateMoveAction(healer.ActorId, moveId, TargetType.Ally, AttackRange.Single)],
            [healMove]));

        var healerState = resolution.UpdatedStates.Single(x => x.Id.Value == healer.ActorId);
        var allyState = resolution.UpdatedStates.Single(x => x.Id.Value == ally.ActorId);

        healerState.CurrentMp.Should().Be(7);
        allyState.CurrentHp.Should().Be(25);
    }

    [Fact]
    public void ResolveTurn_WhenGuardStanceIsUsed_AppliesTauntAndDefenseBuff()
    {
        var service = new BattleService();
        const int moveId = 104;
        var guardMove = CreateGuardStanceMove(moveId);
        var actor = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], speed: 10);

        var resolution = service.ResolveTurn(new BattleTurnRequest(
            [actor],
            [CreateMoveAction(actor.ActorId, moveId, TargetType.Self, AttackRange.Single)],
            [guardMove]));

        var actorState = resolution.UpdatedStates.Single();
        resolution.ActionResults.Single().TargetResults.Should().ContainSingle(x => x.AppliedAilment == AilmentType.Taunt);
        actorState.Ailments.Should().BeEmpty();
        actorState.Buffs.Should().ContainSingle(x => x.Stat == BuffStat.Defense && x.Value == 15m);
    }

    [Fact]
    public void ResolveTurn_WhenGuardStanceWasUsed_PreviousTurnDefenseBuffReducesNextTurnDamage()
    {
        var service = new BattleService();
        const int moveId = 104;
        var guardMove = CreateGuardStanceMove(moveId);
        var actor = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], defense: 5, speed: 10);
        var enemy = CreateActorInput(2, BattleSide.Enemy, currentHp: 30, currentMp: 10, strength: 10, speed: 5);
        var actors = new[] { actor, enemy };

        var turn1 = service.ResolveTurn(new BattleTurnRequest(
            actors,
            [CreateMoveAction(actor.ActorId, moveId, TargetType.Self, AttackRange.Single)],
            [guardMove]));

        actors = CarryForwardActors(actors, turn1.UpdatedStates);

        var turn2 = service.ResolveTurn(new BattleTurnRequest(
            actors,
            [CreateNormalAttackAction(enemy.ActorId)],
            [guardMove]));

        turn2.UpdatedStates.Single(x => x.Id.Value == actor.ActorId).CurrentHp.Should().Be(29);
    }

    [Fact]
    public void ResolveTurn_WhenSpeedBuffMoveIsUsed_SameTurnOrderDoesNotChangeButNextTurnActsFirst()
    {
        var service = new BattleService();
        const int moveId = 108;
        var speedMove = CreateSpeedBuffMove(moveId);
        var actor = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], speed: 5);
        var enemy = CreateActorInput(2, BattleSide.Enemy, currentHp: 30, currentMp: 10, speed: 10);
        var actors = new[] { actor, enemy };

        var turn1 = service.ResolveTurn(new BattleTurnRequest(
            actors,
            [CreateMoveAction(actor.ActorId, moveId, TargetType.Self, AttackRange.Single), CreateNormalAttackAction(enemy.ActorId)],
            [speedMove]));

        turn1.ActionResults[0].ActorId.Value.Should().Be(enemy.ActorId);

        actors = CarryForwardActors(actors, turn1.UpdatedStates);

        var turn2 = service.ResolveTurn(new BattleTurnRequest(
            actors,
            [CreateNormalAttackAction(actor.ActorId), CreateNormalAttackAction(enemy.ActorId)],
            [speedMove]));

        turn2.ActionResults[0].ActorId.Value.Should().Be(actor.ActorId);
    }

    [Fact]
    public void ResolveTurn_WhenDefenseScaledMoveIsUsed_DealsDamageUsingDefense()
    {
        var service = new BattleService();
        const int moveId = 105;
        var bashMove = CreateDefenseScaledMove(moveId);
        var actor = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], defense: 16, speed: 10);
        var enemy = CreateActorInput(2, BattleSide.Enemy, currentHp: 30, currentMp: 10, defense: 5, speed: 5);

        var resolution = service.ResolveTurn(new BattleTurnRequest(
            [actor, enemy],
            [CreateMoveAction(actor.ActorId, moveId, TargetType.Enemy, AttackRange.Single)],
            [bashMove]));

        resolution.UpdatedStates.Single(x => x.Id.Value == enemy.ActorId).CurrentHp.Should().Be(14);
    }

    [Fact]
    public void ResolveTurn_WhenFireAcrossColumnsMoveIsUsed_HitsTwoEnemies()
    {
        var service = new BattleService();
        const int moveId = 106;
        var fireMove = CreateFireAcrossColumnsMove(moveId);
        var actor = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], intelligence: 14, speed: 10);
        var enemy1 = CreateActorInput(2, BattleSide.Enemy, currentHp: 30, currentMp: 10, defense: 4, intelligence: 2, speed: 5);
        var enemy2 = CreateActorInput(3, BattleSide.Enemy, currentHp: 30, currentMp: 10, defense: 4, intelligence: 2, speed: 4);

        var resolution = service.ResolveTurn(new BattleTurnRequest(
            [actor, enemy1, enemy2],
            [CreateMoveAction(actor.ActorId, moveId, TargetType.Enemy, AttackRange.Row)],
            [fireMove]));

        resolution.ActionResults.Single().TargetResults.Should().HaveCount(2);
        resolution.UpdatedStates.Single(x => x.Id.Value == enemy1.ActorId).CurrentHp.Should().BeLessThan(30);
        resolution.UpdatedStates.Single(x => x.Id.Value == enemy2.ActorId).CurrentHp.Should().BeLessThan(30);
    }

    [Fact]
    public void ResolveTurn_WhenParalyzeMoveIsUsed_AppliesParalysis()
    {
        var service = new BattleService();
        const int moveId = 107;
        var paralyzeMove = CreateParalyzeMove(moveId);
        var actor = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], intelligence: 10, speed: 10);
        var enemy = CreateActorInput(2, BattleSide.Enemy, currentHp: 30, currentMp: 10, speed: 5);

        var resolution = service.ResolveTurn(new BattleTurnRequest(
            [actor, enemy],
            [CreateMoveAction(actor.ActorId, moveId, TargetType.Enemy, AttackRange.Single)],
            [paralyzeMove]));

        resolution.ActionResults.Single().TargetResults.Should().ContainSingle(x => x.AppliedAilment == AilmentType.Paralysis);
        resolution.UpdatedStates.Single(x => x.Id.Value == enemy.ActorId).Ailments.Should().BeEmpty();
    }

    [Fact]
    public void ResolveTurn_WhenParalysisWasApplied_RemainsUntilNextTurnAndActionIsResolvedThroughBattleService()
    {
        var service = new BattleService();
        const int moveId = 107;
        var paralyzeMove = CreateParalyzeMove(moveId, turns: 2);
        var actor = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], intelligence: 10, speed: 10);
        var enemy = CreateActorInput(2, BattleSide.Enemy, currentHp: 30, currentMp: 10, speed: 5);
        var actors = new[] { actor, enemy };

        var turn1 = service.ResolveTurn(new BattleTurnRequest(
            actors,
            [CreateMoveAction(actor.ActorId, moveId, TargetType.Enemy, AttackRange.Single)],
            [paralyzeMove]));

        turn1.UpdatedStates.Single(x => x.Id.Value == enemy.ActorId)
            .Ailments.Should().ContainSingle(x => x.Type == AilmentType.Paralysis && x.RemainingTurns == 1);

        actors = CarryForwardActors(actors, turn1.UpdatedStates);

        var turn2 = service.ResolveTurn(new BattleTurnRequest(
            actors,
            [CreateNormalAttackAction(enemy.ActorId)],
            [paralyzeMove]));

        turn2.ActionResults.Single().ActorId.Value.Should().Be(enemy.ActorId);
        turn2.UpdatedStates.Single(x => x.Id.Value == actor.ActorId).CurrentHp.Should().BeInRange(25, 30);
    }

    [Fact]
    public void ResolveTurn_WhenDamageTrapIsApplied_TriggersAcrossTurnsUntilItExpires()
    {
        var service = new BattleService();
        const int moveId = 101;
        var trapMove = CreateDamageTrapMove(moveId);
        var ally = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], strength: 10, speed: 10);
        var enemy = CreateActorInput(2, BattleSide.Enemy, currentHp: 20, strength: 5, speed: 5);
        var actors = new[] { ally, enemy };

        var turn1 = service.ResolveTurn(new BattleTurnRequest(
            actors,
            [CreateMoveAction(ally.ActorId, moveId, TargetType.Enemy, AttackRange.Single)],
            [trapMove]));

        var enemyAfterTurn1 = turn1.UpdatedStates.Single(x => x.Id.Value == enemy.ActorId);
        enemyAfterTurn1.CurrentHp.Should().Be(16);
        enemyAfterTurn1.Ailments.Should().ContainSingle(x => x.Type == AilmentType.DamageTrap && x.RemainingTurns == 1);

        actors = CarryForwardActors(actors, turn1.UpdatedStates);

        var turn2 = service.ResolveTurn(new BattleTurnRequest(
            actors,
            [CreateWaitAction(ally.ActorId), CreateWaitAction(enemy.ActorId)],
            [trapMove]));

        var enemyAfterTurn2 = turn2.UpdatedStates.Single(x => x.Id.Value == enemy.ActorId);
        enemyAfterTurn2.CurrentHp.Should().Be(13);
        enemyAfterTurn2.Ailments.Should().BeEmpty();
    }

    [Fact]
    public void ResolveTurn_WhenPoisonTicksAfterAction_DefeatsEnemy()
    {
        var service = new BattleService();
        const int moveId = 102;
        var poisonMove = CreatePoisonMove(moveId);
        var ally = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId], strength: 10, speed: 10);
        var enemy = CreateActorInput(2, BattleSide.Enemy, currentHp: 1, strength: 5, speed: 5);

        var resolution = service.ResolveTurn(new BattleTurnRequest(
            [ally, enemy],
            [CreateMoveAction(ally.ActorId, moveId, TargetType.Enemy, AttackRange.Single)],
            [poisonMove]));

        var enemyState = resolution.UpdatedStates.Single(x => x.Id.Value == enemy.ActorId);
        enemyState.CurrentHp.Should().Be(0);
        enemyState.IsDead.Should().BeTrue();
        resolution.ActionResults[0].TargetResults.Should().ContainSingle(x => x.AppliedAilment == AilmentType.Poison);
    }

    private static BattleActorInput[] CarryForwardActors(
        IReadOnlyList<BattleActorInput> actors,
        IEnumerable<BattleActorState> updatedStates)
    {
        var stateMap = updatedStates.ToDictionary(x => x.Id.Value);

        return actors.Select(actor =>
        {
            var state = stateMap[actor.ActorId];
            return actor with
            {
                CurrentHp = state.CurrentHp,
                CurrentMp = state.CurrentMp,
                Ailments = state.Ailments.Select(x => new BattleAilmentState(x.Type, x.RemainingTurns, x.TriggerDamage)).ToArray(),
                Buffs = state.Buffs.Select(x => new BattleBuffState(x.Stat, x.CalculationType, x.Value, x.RemainingTurns)).ToArray()
            };
        }).ToArray();
    }

    [Fact]
    public void ResolveTurn_WhenRangeAttackHitsSingleTarget_DoesNotReduceDamage()
    {
        var service = new BattleService();
        const int moveId = 100;
        var wideSlash = CreateWideAttackMove(moveId);
        var ally = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId]);
        var enemy = CreateActorInput(2, BattleSide.Enemy, currentHp: 30);

        var resolution = service.ResolveTurn(new BattleTurnRequest(
            [ally, enemy],
            [CreateMoveAction(ally.ActorId, moveId, TargetType.Enemy, AttackRange.All)],
            [wideSlash]));

        // 固定ダメージ25 - 防御5 = 20。対象1体なので逓減しない。
        resolution.ActionResults[0].TargetResults.Should().ContainSingle();
        resolution.ActionResults[0].TargetResults[0].Damage.Should().Be(20);
    }

    [Fact]
    public void ResolveTurn_WhenRangeAttackHitsFourTargets_ReducesDamagePerTarget()
    {
        var service = new BattleService();
        const int moveId = 100;
        var wideSlash = CreateWideAttackMove(moveId);
        var ally = CreateActorInput(1, BattleSide.Ally, currentHp: 30, currentMp: 10, learnedMoveIds: [moveId]);
        var enemies = new[]
        {
            CreateActorInput(2, BattleSide.Enemy, currentHp: 30),
            CreateActorInput(3, BattleSide.Enemy, currentHp: 30),
            CreateActorInput(4, BattleSide.Enemy, currentHp: 30),
            CreateActorInput(5, BattleSide.Enemy, currentHp: 30)
        };

        var resolution = service.ResolveTurn(new BattleTurnRequest(
            [ally, .. enemies],
            [CreateMoveAction(ally.ActorId, moveId, TargetType.Enemy, AttackRange.All)],
            [wideSlash]));

        // 素のダメージ20 に 1/√4 = 0.5 が掛かる。
        resolution.ActionResults[0].TargetResults.Should().HaveCount(4);
        resolution.ActionResults[0].TargetResults.Should().OnlyContain(x => x.Damage == 10);
    }

    private static BattleActorInput CreateActorInput(
        int seed,
        BattleSide side,
        int currentHp,
        int currentMp = 0,
        int strength = 10,
        int defense = 5,
        int intelligence = 3,
        int luck = 3,
        int speed = 8,
        IReadOnlyList<int>? learnedMoveIds = null)
    {
        return new BattleActorInput(
            ActorId: Guid.Parse($"20000000-0000-0000-0000-{seed:D12}"),
            DisplayName: $"Actor{seed}",
            Side: side,
            BaseStatus: new Status(maxHp: 30, maxMp: 10, strength: strength, defense: defense, intelligence: intelligence, luck: luck, speed: speed),
            MoveSet: CreateMoveSet(learnedMoveIds ?? []),
            CurrentHp: currentHp,
            CurrentMp: currentMp);
    }

    private static MoveSet CreateMoveSet(IReadOnlyList<int> moveIds)
    {
        var slots = new MoveId?[MoveSet.MaxSlots];
        for (var i = 0; i < moveIds.Count; i++)
        {
            slots[i] = new MoveId(moveIds[i]);
        }

        return new MoveSet(slots);
    }

    private static BattleActionInput CreateNormalAttackAction(Guid actorId)
        => new(actorId, BattleActionKind.NormalAttack, null, TargetType.Enemy, AttackRange.Single);

    private static BattleActionInput CreateWaitAction(Guid actorId)
        => new(actorId, BattleActionKind.Wait, null, TargetType.Self, AttackRange.Single);

    private static BattleActionInput CreateMoveAction(Guid actorId, int moveId, TargetType targetType, AttackRange attackRange)
        => new(actorId, BattleActionKind.UseMove, moveId, targetType, attackRange);

    private static Move CreateWideAttackMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "ワイドスラッシュ",
            "敵全体を斬る",
            TargetType.Enemy,
            AttackRange.All,
            3,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Damage,
                    // 範囲攻撃は対象数で威力が逓減するため、2体でも倒しきれる固定値にしている。
                    damage: new DamageEffect(1, 0m, 25, 0m, ElementType.Slash))
            ]);
    }

    private static Move CreateDamageTrapMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "トラップセット",
            "敵単体に罠を仕掛ける",
            TargetType.Enemy,
            AttackRange.Single,
            3,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(
                        AilmentType.DamageTrap,
                        1m,
                        2,
                        new DamageEffect(2, 0.10m, 0, 0m, ElementType.None)))
            ]);
    }

    private static Move CreateHealMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "テラピー",
            "味方単体を回復",
            TargetType.Ally,
            AttackRange.Single,
            3,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Heal,
                    damage: new DamageEffect(1, 1.0m, 5, 0m, ElementType.Holy))
            ]);
    }

    private static Move CreateGuardStanceMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "護符の構え",
            "狙いを集めつつ防御を上げる",
            TargetType.Self,
            AttackRange.Single,
            6,
            1,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(AilmentType.Taunt, 1m, 1)),
                new MoveEffect(
                    new MoveEffectId(2),
                    new MoveId(moveId),
                    2,
                    MoveEffectType.Buff,
                    buff: new BuffEffect(BuffStat.Defense, BuffCalculationType.Add, 15m, 3, 1m, false))
            ]);
    }

    private static Move CreateDefenseScaledMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "シールドバッシュ",
            "防御で殴る",
            TargetType.Enemy,
            AttackRange.Single,
            5,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(1, 1.20m, 2, 0.03m, ElementType.Strike, BuffStat.Defense))
            ]);
    }

    private static Move CreateFireAcrossColumnsMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "プチプラーミヤ",
            "敵横一列を焼く",
            TargetType.Enemy,
            AttackRange.Row,
            6,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(1, 1.25m, 4, 0.02m, ElementType.Fire))
            ]);
    }

    private static Move CreateSpeedBuffMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "アクセル",
            "自分の速度を上げる",
            TargetType.Self,
            AttackRange.Single,
            3,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Buff,
                    buff: new BuffEffect(BuffStat.Speed, BuffCalculationType.Add, 10m, 2, 1m, false))
            ]);
    }

    private static Move CreatePoisonMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            "ポイズン",
            "毒を与える",
            TargetType.Enemy,
            AttackRange.Single,
            2,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(AilmentType.Poison, 1m, 1))
            ]);
    }

    private static Move CreateParalyzeMove(int moveId, int turns = 1)
    {
        return new Move(
            new MoveId(moveId),
            "パラライズ",
            "敵を麻痺させる",
            TargetType.Enemy,
            AttackRange.Single,
            5,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(AilmentType.Paralysis, 1m, turns))
            ]);
    }
}
