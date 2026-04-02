using server.domain.player;

namespace server.domain.quest;

public class QuestStageItemRewardEntry(ItemId? itemId, int weight, bool isMiss)
{
    public ItemId? ItemId { get; } = itemId;
    public int Weight { get; } = weight > 0 ? weight : throw new ArgumentOutOfRangeException(nameof(weight), "weight は1以上である必要があります。");
    public bool IsMiss { get; } = isMiss;
}
