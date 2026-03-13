using server.domain.battle;
using server.domain.battle.enums;

namespace server.domain.quest;

public class FormationLayout(IEnumerable<BattlePosition>? occupiedPositions = null)
{
    private readonly HashSet<BattlePosition> occupiedPositions = occupiedPositions?.ToHashSet() ?? [];

    public IReadOnlyList<BattlePosition> OccupiedPositions => occupiedPositions
        .OrderBy(x => (int)x.Row)
        .ThenBy(x => (int)x.Column)
        .ToArray();

    public void Assign(QuestParticipantId participantId, BattlePosition position)
    {
        _ = participantId;
        ValidatePosition(position);
        if (!occupiedPositions.Add(position))
        {
            throw new InvalidOperationException($"指定された配置はすでに使用中です。 position={position.Row}-{position.Column}");
        }
    }

    public void Release(BattlePosition position)
    {
        occupiedPositions.Remove(position);
    }

    public bool IsOccupied(BattlePosition position)
    {
        return occupiedPositions.Contains(position);
    }

    public BattlePosition FindFirstEmpty(BattleRow? preferredRow = null)
    {
        var candidates = GetAllPositions();
        if (preferredRow is not null)
        {
            var preferred = candidates.FirstOrDefault(x => x.Row == preferredRow && !occupiedPositions.Contains(x));
            if (preferred != default)
            {
                return preferred;
            }
        }

        var fallback = candidates.FirstOrDefault(x => !occupiedPositions.Contains(x));
        if (fallback == default)
        {
            throw new InvalidOperationException("空き配置がありません。");
        }

        return fallback;
    }

    private static BattlePosition[] GetAllPositions()
    {
        return
        [
            new BattlePosition(BattleRow.Front, BattleColumn.Left),
            new BattlePosition(BattleRow.Front, BattleColumn.Right),
            new BattlePosition(BattleRow.Middle, BattleColumn.Left),
            new BattlePosition(BattleRow.Middle, BattleColumn.Right),
            new BattlePosition(BattleRow.Back, BattleColumn.Left),
            new BattlePosition(BattleRow.Back, BattleColumn.Right)
        ];
    }

    private static void ValidatePosition(BattlePosition position)
    {
        _ = position.Row switch
        {
            BattleRow.Front or BattleRow.Middle or BattleRow.Back => position.Row,
            _ => throw new ArgumentOutOfRangeException(nameof(position), "未対応の行です。")
        };

        _ = position.Column switch
        {
            BattleColumn.Left or BattleColumn.Right => position.Column,
            _ => throw new ArgumentOutOfRangeException(nameof(position), "未対応の列です。")
        };
    }
}
