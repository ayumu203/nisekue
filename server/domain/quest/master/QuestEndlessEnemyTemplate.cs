using server.domain.move;
using server.domain.player;
using server.domain.quest.enums;

namespace server.domain.quest;

/// <summary>
/// エンドレスの敵テンプレ（endless_enemy_templates.csv 由来）。テーマ帯 × アーキタイプの
/// フロア1相当 基準ステ(S_base)を保持し、フロア生成時に f(n) を乗じてスケールする。
/// </summary>
public class QuestEndlessEnemyTemplate(
    QuestEnemyDefinitionId id,
    int themeNo,
    EnemyAiType archetype,
    bool isBoss,
    string name,
    Status baseStatus,
    string imagePath,
    IEnumerable<MoveId> moveIds)
{
    private readonly MoveId[] moveIds = moveIds?.ToArray() ?? throw new ArgumentNullException(nameof(moveIds));

    public QuestEnemyDefinitionId Id { get; } = id;
    public int ThemeNo { get; } = ValidatePositive(themeNo, nameof(themeNo));
    public EnemyAiType Archetype { get; } = archetype;
    public bool IsBoss { get; } = isBoss;
    public string Name { get; } = ValidateText(name, nameof(name));
    public Status BaseStatus { get; } = baseStatus ?? throw new ArgumentNullException(nameof(baseStatus));
    public string ImagePath { get; } = ValidateText(imagePath, nameof(imagePath));
    public IReadOnlyList<MoveId> MoveIds => moveIds;

    private static int ValidatePositive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "1以上である必要があります。");
        }

        return value;
    }

    private static string ValidateText(string value, string paramName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("値は必須です。", paramName);
        }

        return normalized;
    }
}
