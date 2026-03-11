namespace server.infrastructure.quest.run;

public class QuestFloorTrapEntity
{
    public Guid RunId { get; set; }
    public Guid TrapId { get; set; }
    public Guid SourceParticipantId { get; set; }
    public int MoveId { get; set; }
    public int ExpiresAfterFloorNo { get; set; }
    public bool IsTriggered { get; set; }
}
