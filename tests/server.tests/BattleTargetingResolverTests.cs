using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move.enums;
using server.domain.player;
using Xunit;

namespace server.tests;

public class BattleTargetingResolverTests
{
    [Fact]
    public void ResolveTargets_WithoutFieldContext_UsesLegacyAttackRangeBehavior()
    {
        var resolver = new BattleTargetingResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var enemy1 = CreateSnapshot(2, BattleSide.Enemy);
        var enemy2 = CreateSnapshot(3, BattleSide.Enemy);
        var enemy3 = CreateSnapshot(4, BattleSide.Enemy);

        var result = resolver.ResolveTargets(
            new BattleTargetSelector(TargetType.Enemy, AttackRange.Row),
            actor,
            [actor, enemy1, enemy2, enemy3],
            CreateStates(actor, enemy1, enemy2, enemy3));

        result.Should().Equal(enemy1.Id, enemy2.Id);
    }

    [Fact]
    public void ResolveTargets_WithFormationRangeControl_FrontCanOnlyTargetFrontRow()
    {
        var resolver = new BattleTargetingResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var enemyFront = CreateSnapshot(2, BattleSide.Enemy);
        var enemyMiddle = CreateSnapshot(3, BattleSide.Enemy);
        var enemyBack = CreateSnapshot(4, BattleSide.Enemy);

        var result = resolver.ResolveTargets(
            new BattleTargetSelector(TargetType.Enemy, AttackRange.All),
            actor,
            [actor, enemyFront, enemyMiddle, enemyBack],
            CreateStates(actor, enemyFront, enemyMiddle, enemyBack),
            new BattleFieldContext([
                new BattleActorPosition(actor.Id, new BattlePosition(BattleRow.Front, BattleColumn.Left)),
                new BattleActorPosition(enemyFront.Id, new BattlePosition(BattleRow.Front, BattleColumn.Left)),
                new BattleActorPosition(enemyMiddle.Id, new BattlePosition(BattleRow.Middle, BattleColumn.Left)),
                new BattleActorPosition(enemyBack.Id, new BattlePosition(BattleRow.Back, BattleColumn.Left))
            ]));

        result.Should().Equal(enemyFront.Id);
    }

    [Fact]
    public void ResolveTargets_WithFormationRangeControl_MiddleCanTargetFrontAndMiddleRows()
    {
        var resolver = new BattleTargetingResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var enemyFront = CreateSnapshot(2, BattleSide.Enemy);
        var enemyMiddle = CreateSnapshot(3, BattleSide.Enemy);
        var enemyBack = CreateSnapshot(4, BattleSide.Enemy);

        var result = resolver.ResolveTargets(
            new BattleTargetSelector(TargetType.Enemy, AttackRange.All),
            actor,
            [actor, enemyFront, enemyMiddle, enemyBack],
            CreateStates(actor, enemyFront, enemyMiddle, enemyBack),
            new BattleFieldContext([
                new BattleActorPosition(actor.Id, new BattlePosition(BattleRow.Middle, BattleColumn.Left)),
                new BattleActorPosition(enemyFront.Id, new BattlePosition(BattleRow.Front, BattleColumn.Left)),
                new BattleActorPosition(enemyMiddle.Id, new BattlePosition(BattleRow.Middle, BattleColumn.Left)),
                new BattleActorPosition(enemyBack.Id, new BattlePosition(BattleRow.Back, BattleColumn.Left))
            ]));

        result.Should().Equal(enemyFront.Id, enemyMiddle.Id);
    }

    [Fact]
    public void ResolveTargets_WithFormationRangeControl_RowTargetsSameRowAcrossColumns()
    {
        var resolver = new BattleTargetingResolver();
        var actor = CreateSnapshot(1, BattleSide.Ally);
        var enemyFrontLeft = CreateSnapshot(2, BattleSide.Enemy);
        var enemyFrontRight = CreateSnapshot(3, BattleSide.Enemy);
        var enemyMiddleLeft = CreateSnapshot(4, BattleSide.Enemy);

        var result = resolver.ResolveTargets(
            new BattleTargetSelector(TargetType.Enemy, AttackRange.Row),
            actor,
            [actor, enemyFrontLeft, enemyFrontRight, enemyMiddleLeft],
            CreateStates(actor, enemyFrontLeft, enemyFrontRight, enemyMiddleLeft),
            new BattleFieldContext([
                new BattleActorPosition(actor.Id, new BattlePosition(BattleRow.Back, BattleColumn.Left)),
                new BattleActorPosition(enemyFrontLeft.Id, new BattlePosition(BattleRow.Front, BattleColumn.Left)),
                new BattleActorPosition(enemyFrontRight.Id, new BattlePosition(BattleRow.Front, BattleColumn.Right)),
                new BattleActorPosition(enemyMiddleLeft.Id, new BattlePosition(BattleRow.Middle, BattleColumn.Left))
            ]));

        result.Should().Equal(enemyFrontLeft.Id, enemyFrontRight.Id);
    }

    private static BattleActorSnapshot CreateSnapshot(int seed, BattleSide side)
    {
        return new BattleActorSnapshot(
            new BattleActorId(Guid.Parse($"00000000-0000-0000-0000-{seed:D12}")),
            $"Actor{seed}",
            side,
            new Status(maxHp: 30, maxMp: 10, strength: 10, defense: 5, intelligence: 3, luck: 3, speed: 8),
            new MoveSet());
    }

    private static IReadOnlyList<BattleActorState> CreateStates(params BattleActorSnapshot[] snapshots)
    {
        return snapshots
            .Select(x => new BattleActorState(x.Id, x.BaseStatus.MaxHp, x.BaseStatus.MaxMp))
            .ToArray();
    }
}
