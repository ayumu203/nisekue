using server.domain.player;

namespace server.infrastructure.chat;

public class ChatMessageEntity(PlayerId owner_id, int chat_id, PlayerId sender_id, string message, DateTimeOffset created_at)
{
	public PlayerId Owner_id { get; } = owner_id;
	public int Chat_id { get; } = chat_id;
	public PlayerId Sender_id { get; } = sender_id;
	public string Message { get; } = message;
	public DateTimeOffset Created_at { get; } = created_at;
}