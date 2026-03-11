using server.domain.move;
using server.domain.player;
using server.domain.quest.enums;

namespace server.domain.quest;

public class QuestEnemyDefinition(
    QuestEnemyDefinitionId id,
    string name,
    int level,
    Status status,
    string imagePath,
    EnemyAiType aiType,
    IEnumerable<MoveId> moveIds)
{
    private readonly MoveId[] moveIds = moveIds?.ToArray() ?? throw new ArgumentNullException(nameof(moveIds));

    public QuestEnemyDefinitionId Id { get; } = id;
    public string Name { get; } = ValidateText(name, nameof(name));
    public int Level { get; } = ValidateLevel(level);
    public Status Status { get; } = status ?? throw new ArgumentNullException(nameof(status));
    public string ImagePath { get; } = ValidateText(imagePath, nameof(imagePath));
    public EnemyAiType AiType { get; } = aiType;
    public IReadOnlyList<MoveId> MoveIds => moveIds;

    private static string ValidateText(string value, string paramName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("画像パスは必須です。", paramName);
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
