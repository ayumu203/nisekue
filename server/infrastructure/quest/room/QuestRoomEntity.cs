namespace server.infrastructure.quest.room;

public class QuestRoomEntity
{
    public Guid Id { get; set; }
    public Guid OwnerPlayerId { get; set; }
    public int StageId { get; set; }
    public int Mode { get; set; }
    public int Status { get; set; }
    public int? MinRequiredLevel { get; set; }
    public int Version { get; set; }
    public int? CloseReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
