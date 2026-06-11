using server.domain.player;

namespace server.domain.quest;

public class QuestRewardAccumulator(
    int exp = 0,
    EquipmentId? equipmentRewardId = null,
    ItemId? itemRewardId = null,
    int gold = 0,
    IEnumerable<PlayerId>? skippedRewardPlayerIds = null,
    IEnumerable<QuestCapturedPetReward>? capturedPetRewards = null)
{
    public int Exp { get; private set; } = ValidateNonNegative(exp);
    public int Gold { get; private set; } = ValidateNonNegative(gold);
    private readonly HashSet<PlayerId> skippedRewardPlayerIds = skippedRewardPlayerIds?.ToHashSet() ?? [];
    private readonly List<QuestCapturedPetReward> capturedPetRewards = capturedPetRewards?.ToList() ?? [];
    public EquipmentId? EquipmentRewardId { get; private set; } = equipmentRewardId;
    public ItemId? ItemRewardId { get; private set; } = itemRewardId;
    public IReadOnlySet<PlayerId> SkippedRewardPlayerIds => skippedRewardPlayerIds;
    public IReadOnlyList<QuestCapturedPetReward> CapturedPetRewards => capturedPetRewards;

    public void AddExp(int value)
    {
        Exp += ValidateNonNegative(value);
    }

    public void AddGold(int value)
    {
        Gold += ValidateNonNegative(value);
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

    public void AddCapturedPetReward(QuestCapturedPetReward reward)
    {
        ArgumentNullException.ThrowIfNull(reward);
        capturedPetRewards.Add(reward);
    }

    public int CountCapturedPetsByPlayer(PlayerId playerId)
    {
        return capturedPetRewards.Count(x => x.PlayerId == playerId);
    }

    public void ClearCapturedPetRewards()
    {
        capturedPetRewards.Clear();
    }

    public void SetCapturedPetRewards(IEnumerable<QuestCapturedPetReward> rewards)
    {
        ArgumentNullException.ThrowIfNull(rewards);
        capturedPetRewards.Clear();
        capturedPetRewards.AddRange(rewards);
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
