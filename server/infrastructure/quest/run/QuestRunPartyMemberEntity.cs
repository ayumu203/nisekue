namespace server.infrastructure.quest.run;

public class QuestRunPartyMemberEntity
{
    public Guid RunId { get; set; }
    public Guid ParticipantId { get; set; }
    public int CurrentHp { get; set; }
    public int CurrentMp { get; set; }
    public bool IsDead { get; set; }
    public int CanActFromTurn { get; set; }
    public int ActionMode { get; set; }
    public bool HasLeftQuest { get; set; }
    public bool IsManualControlRequested { get; set; }
    public string ActiveEffectsJson { get; set; } = "{}";
    public string DerivedParametersJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; }
}
