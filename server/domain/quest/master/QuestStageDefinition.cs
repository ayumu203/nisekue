namespace server.domain.quest;

public class QuestStageDefinition(
    QuestStageId id,
    string stageCode,
    string name,
    string battlefieldImagePath,
    int recommendedLevel,
    int? minimumEntryLevel,
    int minPartyMemberCount,
    int maxPartyMemberCount,
    IEnumerable<QuestFloorDefinition> floors,
    IEnumerable<QuestStageEquipmentRewardEntry>? equipmentRewards,
    IEnumerable<QuestStageItemRewardEntry>? itemRewards,
    bool isActive)
{
    private readonly QuestFloorDefinition[] floors = floors?.OrderBy(x => x.FloorNo).ToArray()
        ?? throw new ArgumentNullException(nameof(floors));
    private readonly QuestStageEquipmentRewardEntry[] equipmentRewards = equipmentRewards?.ToArray() ?? [];
    private readonly QuestStageItemRewardEntry[] itemRewards = itemRewards?.ToArray() ?? [];

    public QuestStageId Id { get; } = id;
    public string StageCode { get; } = ValidateText(stageCode, nameof(stageCode));
    public string Name { get; } = ValidateText(name, nameof(name));
    public string BattlefieldImagePath { get; } = ValidateText(battlefieldImagePath, nameof(battlefieldImagePath));
    public int RecommendedLevel { get; } = ValidateNonNegative(recommendedLevel, nameof(recommendedLevel));
    public int? MinimumEntryLevel { get; } = ValidateNullablePositive(minimumEntryLevel, nameof(minimumEntryLevel));
    public int MinPartyMemberCount { get; } = ValidatePartyCount(minPartyMemberCount, nameof(minPartyMemberCount));
    public int MaxPartyMemberCount { get; } = ValidatePartyCount(maxPartyMemberCount, nameof(maxPartyMemberCount));
    public IReadOnlyList<QuestFloorDefinition> Floors => floors;
    public IReadOnlyList<QuestStageEquipmentRewardEntry> EquipmentRewards => equipmentRewards;
    public IReadOnlyList<QuestStageItemRewardEntry> ItemRewards => itemRewards;
    public bool IsActive { get; } = isActive;

    private static string ValidateText(string value, string paramName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("値は必須です。", paramName);
        }

        return normalized;
    }

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "0以上である必要があります。");
        }

        return value;
    }

    private static int ValidatePartyCount(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "1以上である必要があります。");
        }

        return value;
    }

    private static int? ValidateNullablePositive(int? value, string paramName)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "1以上である必要があります。");
        }

        return value.Value;
    }
}
