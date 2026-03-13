using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;
using server.domain.quest.enums;

namespace server.domain.quest;

public class QuestNpcTemplate(
    QuestNpcTemplateId id,
    string name,
    Job job,
    BattleRow preferredRow,
    int level,
    Status baseStatus,
    IEnumerable<MoveId> moveIds,
    NpcRole role)
{
    private readonly MoveId[] moveIds = moveIds?.ToArray() ?? throw new ArgumentNullException(nameof(moveIds));

    public QuestNpcTemplateId Id { get; } = id;
    public string Name { get; } = ValidateText(name, nameof(name));
    public Job Job { get; } = job;
    public BattleRow PreferredRow { get; } = preferredRow;
    public int Level { get; } = ValidateLevel(level);
    public Status BaseStatus { get; } = baseStatus ?? throw new ArgumentNullException(nameof(baseStatus));
    public IReadOnlyList<MoveId> MoveIds => moveIds;
    public NpcRole Role { get; } = role;

    private static string ValidateText(string value, string paramName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("値は必須です。", paramName);
        }

        return normalized;
    }

    private static int ValidateLevel(int level)
    {
        if (level <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "レベルは1以上である必要があります。");
        }

        return level;
    }
}
