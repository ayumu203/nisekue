using server.domain.player;

namespace server.domain.move;

public sealed class JobMoveLearningRule(Job job, IReadOnlyList<MoveId> moveIds)
{
    public Job Job { get; } = job;
    public IReadOnlyList<MoveId> MoveIds { get; } = moveIds ?? throw new ArgumentNullException(nameof(moveIds));

    public int ResolveRequiredLevel(int order, int masterLevel)
    {
        if (order < 1 || order > MoveIds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(order), "order は1以上、技数以下である必要があります。");
        }

        if (masterLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(masterLevel), "masterLevel は1以上である必要があります。");
        }

        return Math.Max(1, masterLevel * order / MoveIds.Count);
    }

    public IReadOnlyList<MoveId> GetLearnableMoveIds(int level, int masterLevel)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "level は1以上である必要があります。");
        }

        if (MoveIds.Count == 0)
        {
            return [];
        }

        var learnable = new List<MoveId>();
        for (var i = 0; i < MoveIds.Count; i++)
        {
            if (level >= ResolveRequiredLevel(i + 1, masterLevel))
            {
                learnable.Add(MoveIds[i]);
            }
        }

        return learnable;
    }
}
