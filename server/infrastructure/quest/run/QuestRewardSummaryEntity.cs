namespace server.infrastructure.quest.run;

public class QuestRewardSummaryEntity
{
    public Guid RunId { get; set; }
    public int Exp { get; set; }
    public int? EquipmentRewardId { get; set; }
    public string SkippedRewardPlayerIdsJson { get; set; } = "[]";
}
