using server.domain.player;
using server.domain.chat;

namespace server.infrastructure.chat;

public class ChatMessageEntity(
    PlayerId ownerId,
    int chatId,
    ChatMessageSenderType senderType,
    PlayerId? senderId,
    string message,
    DateTimeOffset createdAt,
    bool isAlerted)
{
    public PlayerId OwnerId { get; } = ownerId;
    public int ChatId { get; } = chatId;
    public ChatMessageSenderType SenderType { get; } = senderType;
    public PlayerId? SenderId { get; } = senderId;
    public string Message { get; } = message;
    public DateTimeOffset CreatedAt { get; } = createdAt;
    public bool IsAlerted { get; } = isAlerted;
}
