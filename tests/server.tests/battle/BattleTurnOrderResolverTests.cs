using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using Xunit;

namespace server.tests;

public class BattleTurnOrderResolverTests
{
    [Fact]
    public void Resolve_WhenMoveHasHigherPriority_OrdersByPriorityBeforeSpeed()
    {
        var resolver = new BattleTurnOrderResolver(new BattleStatusResolver());
        var fastActor = CreateSnapshot(1, speed: 20);
        var priorityActor = CreateSnapshot(2, speed: 5);
        var move = CreateMove(moveId: 1, executionPriority: 1);

        var result = resolver.Resolve(
            [
                new BattleAction(fastActor.Id, BattleActionKind.NormalAttack, CreateEnemyTarget()),
                new BattleAction(priorityActor.Id, BattleActionKind.UseMove, CreateEnemyTarget(), new MoveId(421))
            ],
            [fastActor, priorityActor],
            [CreateState(fastActor.Id), CreateState(priorityActor.Id)],
            [move]);

        result.Should().Equal(priorityActor.Id, fastActor.Id);
    }

    [Fact]
    public void Resolve_WhenPrioritySame_OrdersByEffectiveSpeedDescending()
    {
        var resolver = new BattleTurnOrderResolver(new BattleStatusResolver());
        var slowerActor = CreateSnapshot(1, speed: 10);
        var fasterActor = CreateSnapshot(2, speed: 8);
        var fasterState = CreateState(
            fasterActor.Id,
            new BattleBuffState(BuffStat.Speed, BuffCalculationType.Add, 5m, 1));

        var result = resolver.Resolve(
            [
                new BattleAction(slowerActor.Id, BattleActionKind.NormalAttack, CreateEnemyTarget()),
                new BattleAction(fasterActor.Id, BattleActionKind.NormalAttack, CreateEnemyTarget())
            ],
            [slowerActor, fasterActor],
            [CreateState(slowerActor.Id), fasterState],
            []);

        result.Should().Equal(fasterActor.Id, slowerActor.Id);
    }

    [Fact]
    public void Resolve_WhenPriorityAndSpeedSame_OrdersByActorIdAscending()
    {
        var resolver = new BattleTurnOrderResolver(new BattleStatusResolver());
        var actor1 = CreateSnapshot(1, speed: 10);
        var actor2 = CreateSnapshot(2, speed: 10);

        var result = resolver.Resolve(
            [
                new BattleAction(actor2.Id, BattleActionKind.NormalAttack, CreateEnemyTarget()),
                new BattleAction(actor1.Id, BattleActionKind.NormalAttack, CreateEnemyTarget())
            ],
            [actor1, actor2],
            [CreateState(actor1.Id), CreateState(actor2.Id)],
            []);

        result.Should().Equal(actor1.Id, actor2.Id);
    }

    [Fact]
    public void Resolve_WhenActorIsDead_ExcludesActorFromTurnOrder()
    {
        var resolver = new BattleTurnOrderResolver(new BattleStatusResolver());
        var aliveActor = CreateSnapshot(1, speed: 10);
        var deadActor = CreateSnapshot(2, speed: 20);
        var deadState = CreateState(deadActor.Id, currentHp: 0);

        var result = resolver.Resolve(
            [
                new BattleAction(aliveActor.Id, BattleActionKind.NormalAttack, CreateEnemyTarget()),
                new BattleAction(deadActor.Id, BattleActionKind.NormalAttack, CreateEnemyTarget())
            ],
            [aliveActor, deadActor],
            [CreateState(aliveActor.Id), deadState],
            []);

        result.Should().Equal(aliveActor.Id);
    }

    [Fact]
    public void Resolve_WhenActionIsWait_OrdersAfterDefaultPriorityActions()
    {
        var resolver = new BattleTurnOrderResolver(new BattleStatusResolver());
        var waitActor = CreateSnapshot(1, speed: 20);
        var attackActor = CreateSnapshot(2, speed: 5);

        var result = resolver.Resolve(
            [
                new BattleAction(waitActor.Id, BattleActionKind.Wait, CreateSelfTarget()),
                new BattleAction(attackActor.Id, BattleActionKind.NormalAttack, CreateEnemyTarget())
            ],
            [waitActor, attackActor],
            [CreateState(waitActor.Id), CreateState(attackActor.Id)],
            []);

        result.Should().Equal(attackActor.Id, waitActor.Id);
    }

    [Fact]
    public void Resolve_WithMultipleActors_OrdersAllActorsByPriorityThenSpeedThenActorId()
    {
        var resolver = new BattleTurnOrderResolver(new BattleStatusResolver());
        var priorityActor = CreateSnapshot(1, speed: 4);
        var fastActor = CreateSnapshot(2, speed: 15);
        var sameSpeedActor = CreateSnapshot(3, speed: 15);
        var waitActor = CreateSnapshot(4, speed: 30);
        var move = CreateMove(moveId: 1, executionPriority: 1);

        var result = resolver.Resolve(
            [
                new BattleAction(waitActor.Id, BattleActionKind.Wait, CreateSelfTarget()),
                new BattleAction(sameSpeedActor.Id, BattleActionKind.NormalAttack, CreateEnemyTarget()),
                new BattleAction(priorityActor.Id, BattleActionKind.UseMove, CreateEnemyTarget(), new MoveId(421)),
                new BattleAction(fastActor.Id, BattleActionKind.NormalAttack, CreateEnemyTarget())
            ],
            [priorityActor, fastActor, sameSpeedActor, waitActor],
            [CreateState(priorityActor.Id), CreateState(fastActor.Id), CreateState(sameSpeedActor.Id), CreateState(waitActor.Id)],
            [move]);

        result.Should().Equal(priorityActor.Id, fastActor.Id, sameSpeedActor.Id, waitActor.Id);
    }

    [Fact]
    public void Resolve_WithAllyAndEnemyActors_OrdersWithoutConsideringBattleSide()
    {
        var resolver = new BattleTurnOrderResolver(new BattleStatusResolver());
        var allyActor = CreateSnapshot(1, speed: 12, side: BattleSide.Ally);
        var enemyPriorityActor = CreateSnapshot(2, speed: 3, side: BattleSide.Enemy);
        var enemyFastActor = CreateSnapshot(3, speed: 15, side: BattleSide.Enemy);
        var move = CreateMove(moveId: 1, executionPriority: 1);

        var result = resolver.Resolve(
            [
                new BattleAction(allyActor.Id, BattleActionKind.NormalAttack, CreateEnemyTarget()),
                new BattleAction(enemyPriorityActor.Id, BattleActionKind.UseMove, CreateEnemyTarget(), new MoveId(421)),
                new BattleAction(enemyFastActor.Id, BattleActionKind.NormalAttack, CreateEnemyTarget())
            ],
            [allyActor, enemyPriorityActor, enemyFastActor],
            [CreateState(allyActor.Id), CreateState(enemyPriorityActor.Id), CreateState(enemyFastActor.Id)],
            [move]);

        result.Should().Equal(enemyPriorityActor.Id, enemyFastActor.Id, allyActor.Id);
    }

    [Fact]
    public void Resolve_WhenMovePrioritiesRangeFromMinusThreeToThree_OrdersByPriorityDescending()
    {
        var resolver = new BattleTurnOrderResolver(new BattleStatusResolver());
        var priorities = new[] { -3, -2, -1, 0, 1, 2, 3 };
        var actors = priorities
            .Select((priority, index) => CreateSnapshot(index + 1, speed: 10))
            .ToArray();
        var moves = priorities
            .Select((priority, index) => CreateMove(moveId: index + 1, executionPriority: priority))
            .ToArray();
        var actions = actors
            .Zip(moves, (actor, move) => new BattleAction(actor.Id, BattleActionKind.UseMove, CreateEnemyTarget(), move.Id))
            .ToArray();
        var states = actors
            .Select(x => CreateState(x.Id))
            .ToArray();

        var result = resolver.Resolve(actions, actors, states, moves);

        result.Should().Equal(
            actors[6].Id,
            actors[5].Id,
            actors[4].Id,
            actors[3].Id,
            actors[2].Id,
            actors[1].Id,
            actors[0].Id);
    }

    private static BattleActorSnapshot CreateSnapshot(int seed, int speed, BattleSide side = BattleSide.Ally)
    {
        return new BattleActorSnapshot(
            new BattleActorId(Guid.Parse($"00000000-0000-0000-0000-{seed:D12}")),
            $"Actor{seed}",
            side,
            new Status(maxHp: 30, maxMp: 10, strength: 10, defense: 5, intelligence: 3, luck: 3, speed: speed),
            new MoveSet(new MoveId?[]
            {
                new MoveId(421), null, null, null, null, null, null, null, null, null
            }));
    }

    private static BattleActorState CreateState(BattleActorId actorId, BattleBuffState? buff = null, int currentHp = 30)
    {
        var buffs = buff is null ? Array.Empty<BattleBuffState>() : [buff];
        return new BattleActorState(actorId, currentHp: currentHp, currentMp: 10, buffs: buffs);
    }

    private static BattleTargetSelector CreateEnemyTarget()
    {
        return new BattleTargetSelector(TargetType.Enemy, AttackRange.Single);
    }

    private static BattleTargetSelector CreateSelfTarget()
    {
        return new BattleTargetSelector(TargetType.Self, AttackRange.Single);
    }

    private static Move CreateMove(int moveId, int executionPriority)
    {
        return new Move(
            id: new MoveId(moveId),
            name: "テストスキル",
            description: "優先度確認用",
            targetType: TargetType.Enemy,
            attackRange: AttackRange.Single,
            mpCost: 1,
            executionPriority: executionPriority,
            category: MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    effectId: new MoveEffectId(1),
                    moveId: new MoveId(moveId),
                    sequence: 1,
                    effectType: MoveEffectType.Damage,
                    damage: new DamageEffect(hitCount: 1, powerRate: 1m, fixedValue: 1, criticalRate: 0m, elementType: ElementType.None))
            ]);
    }
}
