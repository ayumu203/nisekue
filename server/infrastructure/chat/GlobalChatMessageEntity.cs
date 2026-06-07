using server.domain.chat;
using server.domain.player;

namespace server.infrastructure.chat;

public class GlobalChatMessageEntity(
    int chatId,
    ChatMessageSenderType senderType,
    PlayerId? senderId,
    string message,
    DateTimeOffset createdAt)
{
    public int ChatId { get; } = chatId;
    public ChatMessageSenderType SenderType { get; } = senderType;
    public PlayerId? SenderId { get; } = senderId;
    public string Message { get; } = message;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}
