using server.domain.player;

namespace server.domain.quest;

public class QuestStageEquipmentRewardEntry(EquipmentId? equipmentId, int weight, bool isMiss)
{
    public EquipmentId? EquipmentId { get; } = equipmentId;
    public int Weight { get; } = weight > 0 ? weight : throw new ArgumentOutOfRangeException(nameof(weight), "weight は1以上である必要があります。");
    public bool IsMiss { get; } = isMiss;
}
