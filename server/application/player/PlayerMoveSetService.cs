using server.domain.move;
using server.domain.player;

namespace server.application.player;

public class PlayerMoveSetService(IPlayerRepository playerRepository)
{
    public async Task<Player> UpdateAsync(PlayerId playerId, IReadOnlyList<int?> moveIds)
    {
        ArgumentNullException.ThrowIfNull(moveIds);

        if (moveIds.Count != MoveSet.MaxSlots)
        {
            throw new ArgumentException($"moveIds は {MoveSet.MaxSlots} 件固定です。", nameof(moveIds));
        }

        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

        var currentMoveIds = player.MoveSet.GetLearnedMoveIds()
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToArray();

        var requestedMoveIds = moveIds
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToArray();

        if (requestedMoveIds.Distinct().Count() != requestedMoveIds.Length)
        {
            throw new InvalidOperationException("同じスキルを複数スロットへ配置することはできません。");
        }

        var unknownMoveIds = requestedMoveIds
            .Except(currentMoveIds)
            .ToArray();
        if (unknownMoveIds.Length > 0)
        {
            throw new InvalidOperationException($"現在のスキルセットに存在しない moveId が含まれています。 moveIds={string.Join(',', unknownMoveIds)}");
        }

        var requestedOrdered = requestedMoveIds
            .OrderBy(x => x)
            .ToArray();
        if (!currentMoveIds.SequenceEqual(requestedOrdered))
        {
            throw new InvalidOperationException("現在習得済みのスキル集合と一致する並びのみ保存できます。");
        }

        var normalizedSlots = moveIds
            .Where(x => x.HasValue)
            .Select(x => (MoveId?)new MoveId(x!.Value))
            .Concat(Enumerable.Repeat<MoveId?>(null, MoveSet.MaxSlots - requestedMoveIds.Length))
            .ToArray();

        player.UpdateMoveSet(new MoveSet(normalizedSlots));
        await playerRepository.SaveAsync(player);
        return player;
    }
}
