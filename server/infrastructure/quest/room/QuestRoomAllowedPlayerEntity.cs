namespace server.infrastructure.quest.room;

public class QuestRoomAllowedPlayerEntity
{
    public Guid RoomId { get; set; }
    public Guid PlayerId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}
