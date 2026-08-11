using FluentAssertions;
using server.application.player;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerMoveSetSanitizerTests
{
    [Fact]
    public void TrySanitize_WhenUnknownMoveIdExists_RemovesUnknownMoveAndPacksKnownMoves()
    {
        var slots = new MoveId?[MoveSet.MaxSlots];
        slots[0] = new MoveId(101);
        slots[1] = new MoveId(999);
        slots[2] = new MoveId(102);
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            "Tester",
            level: 5,
            exp: 0,
            jobLevel: 1,
            gold: 100,
            status: new Status(30, 10, 10, 5, 3, 3, 5),
            job: Job.Warrior,
            moveSet: new MoveSet(slots));
        var sanitizer = new PlayerMoveSetSanitizer();

        var changed = sanitizer.TrySanitize(player, [CreateMove(101), CreateMove(102)]);

        changed.Should().BeTrue();
        player.MoveSet.Slots.Select(x => x?.Id).Should().Equal(101, 102, null, null, null, null, null, null, null, null);
    }

    private static Move CreateMove(int moveId)
    {
        return new Move(
            new MoveId(moveId),
            $"Move{moveId}",
            "test move",
            TargetType.Enemy,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(moveId),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(1, 1m, 1, 0m, ElementType.None))
            ]);
    }
}
