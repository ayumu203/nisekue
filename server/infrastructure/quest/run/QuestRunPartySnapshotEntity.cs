namespace server.infrastructure.quest.run;

public class QuestRunPartySnapshotEntity
{
    public Guid RunId { get; set; }
    public Guid ParticipantId { get; set; }
    public int ParticipantType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public int Job { get; set; }
    public int StartRow { get; set; }
    public int StartColumn { get; set; }
    public int MaxHp { get; set; }
    public int MaxMp { get; set; }
    public int Strength { get; set; }
    public int Defense { get; set; }
    public int Intelligence { get; set; }
    public int Luck { get; set; }
    public int Speed { get; set; }
    public string MoveSetJson { get; set; } = "[]";
    public int InitialActionMode { get; set; }
}
