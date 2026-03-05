using server.domain.player;

namespace server.infrastructure.chat;

public class ChatRoomEntity(PlayerId owner_id, int last_chat_id)
{
    public PlayerId Owner_id { get; } = owner_id;
    public int Last_chat_id { get; } = last_chat_id;
}