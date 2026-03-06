using server.domain.player;

namespace server.infrastructure.chat;

public class ChatRoomEntity(PlayerId ownerId, int lastChatId)
{
    public PlayerId OwnerId { get; } = ownerId;
    public int LastChatId { get; } = lastChatId;
}
