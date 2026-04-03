using server.domain.move;
using server.domain.player;

namespace server.application.player;

public sealed class PlayerMoveSetSanitizer
{
    public bool TrySanitize(Player player, IReadOnlyCollection<Move> moves)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(moves);

        var validMoveIds = moves
            .Select(x => x.Id.Id)
            .ToHashSet();
        var seenMoveIds = new HashSet<int>();
        var normalizedSlots = player.MoveSet.Slots
            .Where(x => x is not null && validMoveIds.Contains(x.Id) && seenMoveIds.Add(x.Id))
            .Cast<MoveId?>()
            .Concat(Enumerable.Repeat<MoveId?>(null, MoveSet.MaxSlots))
            .Take(MoveSet.MaxSlots)
            .ToArray();

        var currentSlots = player.MoveSet.Slots.ToArray();
        if (currentSlots.SequenceEqual(normalizedSlots))
        {
            return false;
        }

        player.UpdateMoveSet(new MoveSet(normalizedSlots));
        return true;
    }
}
