namespace server.infrastructure.chat;

public class GlobalChatRoomEntity(Guid id, int lastChatId)
{
    public Guid Id { get; } = id;
    public int LastChatId { get; } = lastChatId;
}
