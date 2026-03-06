using server.domain.player;

namespace server.infrastructure.chat;

public class ChatMessageEntity(PlayerId ownerId, int chatId, PlayerId senderId, string message, DateTimeOffset createdAt)
{
    public PlayerId OwnerId { get; } = ownerId;
    public int ChatId { get; } = chatId;
    public PlayerId SenderId { get; } = senderId;
    public string Message { get; } = message;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}
