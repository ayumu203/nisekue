using server.domain.player;

namespace server.domain.chat;

public interface IChatRoomRepository
{
    Task<ChatRoom> GetChatRoomAsync(PlayerId ownerId);
    Task SaveAsync(ChatRoom room);
}