namespace server.infrastructure.quest.run;

public class QuestRunEntity
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public int StageId { get; set; }
    public int Status { get; set; }
    public int CurrentFloorNo { get; set; }
    public int CurrentTurnNo { get; set; }
    public DateTimeOffset ActionDeadlineAt { get; set; }
    public int? LastResolvedTurnNo { get; set; }
    public string? LastTurnResultsJson { get; set; }
    public string ChatMessagesJson { get; set; } = "[]";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}
