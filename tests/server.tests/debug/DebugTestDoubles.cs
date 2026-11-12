using server.application.player;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;

namespace server.tests;

/// <summary>1 人のプレイヤーをそのまま書き換える IPlayerMutationService。</summary>
internal sealed class SinglePlayerMutationService(Player player) : IPlayerMutationService
{
    public int MutateCount { get; private set; }

    public async Task<TResult> MutateAsync<TResult>(PlayerId playerId, Func<Player, Task<TResult>> mutation)
    {
        if (playerId != player.Id)
        {
            throw new KeyNotFoundException("プレイヤーが見つかりません。");
        }

        MutateCount++;
        return await mutation(player);
    }
}

internal sealed class FixedMoveRepository(params int[] moveIds) : IMoveRepository
{
    private readonly IReadOnlyList<Move> moves = moveIds
        .Select(id => new Move(
            new MoveId(id),
            $"move{id}",
            $"move{id} のテストスキル",
            TargetType.Enemy,
            AttackRange.Single,
            mpCost: 0,
            executionPriority: 0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(id),
                    new MoveId(id),
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(1, 1m, 0, 0m, ElementType.None, null))
            ]))
        .ToArray();

    public Task<Move?> GetMoveAsync(MoveId moveId) => Task.FromResult(moves.FirstOrDefault(x => x.Id.Id == moveId.Id));

    public Task<IReadOnlyList<Move>> GetAllMovesAsync() => Task.FromResult(moves);
}

internal static class DebugTestPlayers
{
    public static Player Create(int level = 5, Job job = Job.Warrior, params int[] moveIds)
    {
        var slots = new MoveId?[MoveSet.MaxSlots];
        for (var i = 0; i < moveIds.Length; i++)
        {
            slots[i] = new MoveId(moveIds[i]);
        }

        return new Player(
            new PlayerId(Guid.NewGuid()),
            "Tester",
            level: level,
            exp: 0,
            jobLevel: 1,
            gold: 100,
            status: new Status(30, 10, 10, 5, 3, 3, 5, accuracy: 120, evasion: 7),
            job: job,
            moveSet: new MoveSet(slots));
    }
}
