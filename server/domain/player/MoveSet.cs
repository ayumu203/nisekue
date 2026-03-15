using server.domain.move;

namespace server.domain.player;

public class MoveSet
{
    public const int MaxSlots = 10;

    private readonly MoveId?[] _slots;
    public IReadOnlyList<MoveId?> Slots => _slots;

    public MoveSet()
    {
        _slots = new MoveId?[MaxSlots];
    }

    public MoveSet(IEnumerable<MoveId?> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        _slots = slots.ToArray();
        if (_slots.Length != MaxSlots)
        {
            throw new ArgumentException($"MoveSet は {MaxSlots} スロット固定です。", nameof(slots));
        }
    }

    public void SetSlot(int index, MoveId? moveId)
    {
        _slots[ValidateIndex(index)] = moveId;
    }

    public MoveId? GetSlot(int index)
    {
        return _slots[ValidateIndex(index)];
    }

    public IReadOnlyList<MoveId> GetLearnedMoveIds()
    {
        return _slots
            .Where(x => x is not null)
            .Select(x => x!)
            .ToArray();
    }

    public bool Contains(MoveId moveId)
    {
        ArgumentNullException.ThrowIfNull(moveId);

        return _slots.Any(x => x?.Id == moveId.Id);
    }

    public void AddLearnedMove(MoveId moveId)
    {
        ArgumentNullException.ThrowIfNull(moveId);

        if (Contains(moveId))
        {
            return;
        }

        var emptyIndex = GetFirstEmptySlotIndex();
        if (emptyIndex is not null)
        {
            _slots[emptyIndex.Value] = moveId;
            return;
        }

        DropOldestMoveBySetLimit();
        _slots[^1] = moveId;
    }

    public int? GetFirstEmptySlotIndex()
    {
        for (var i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] is null)
            {
                return i;
            }
        }

        return null;
    }

    public void ClearSlot(int index)
    {
        _slots[ValidateIndex(index)] = null;
    }

    public void SwapSlots(int fromIndex, int toIndex)
    {
        var from = ValidateIndex(fromIndex);
        var to = ValidateIndex(toIndex);
        (_slots[from], _slots[to]) = (_slots[to], _slots[from]);
    }

    private void DropOldestMoveBySetLimit()
    {
        for (var i = 1; i < _slots.Length; i++)
        {
            _slots[i - 1] = _slots[i];
        }

        _slots[^1] = null;
    }

    private static int ValidateIndex(int index)
    {
        if (index < 0 || index >= MaxSlots)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"index は0以上{MaxSlots - 1}以下である必要があります。");
        }

        return index;
    }
}
