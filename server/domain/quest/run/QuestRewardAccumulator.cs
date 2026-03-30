using server.domain.player;

namespace server.domain.quest;

public class QuestRewardAccumulator(int exp = 0, EquipmentId? equipmentRewardId = null, ItemId? itemRewardId = null, IEnumerable<PlayerId>? skippedRewardPlayerIds = null)
{
    public int Exp { get; private set; } = ValidateNonNegative(exp);
    private readonly HashSet<PlayerId> skippedRewardPlayerIds = skippedRewardPlayerIds?.ToHashSet() ?? [];
    public EquipmentId? EquipmentRewardId { get; private set; } = equipmentRewardId;
    public ItemId? ItemRewardId { get; private set; } = itemRewardId;
    public IReadOnlySet<PlayerId> SkippedRewardPlayerIds => skippedRewardPlayerIds;

    public void AddExp(int value)
    {
        Exp += ValidateNonNegative(value);
    }

    public void SetEquipmentReward(EquipmentId? equipmentId)
    {
        EquipmentRewardId = equipmentId;
    }

    public void SetItemReward(ItemId? itemId)
    {
        ItemRewardId = itemId;
    }

    public void SetSkippedRewardPlayerIds(IEnumerable<PlayerId> playerIds)
    {
        ArgumentNullException.ThrowIfNull(playerIds);
        skippedRewardPlayerIds.Clear();
        foreach (var playerId in playerIds)
        {
            skippedRewardPlayerIds.Add(playerId);
        }
    }

    private static int ValidateNonNegative(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "0以上である必要があります。");
        }

        return value;
    }
}
