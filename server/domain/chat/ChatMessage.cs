using server.domain.player;

namespace server.domain.chat;

public class ChatMessage(ChatMessageSenderType senderType, PlayerId? senderId, int chatId, ChatText body, DateTimeOffset createdAt)
{
    public ChatMessageSenderType SenderType { get; } = senderType;
    public PlayerId? SenderId { get; } = senderType == ChatMessageSenderType.Player
        ? senderId ?? throw new ArgumentNullException(nameof(senderId))
        : senderId;
    public int ChatId { get; } = ValidateChatId(chatId);
    public ChatText Body { get; } = body ?? throw new ArgumentNullException(nameof(body));
    public DateTimeOffset CreatedAt { get; } = createdAt;

    private static int ValidateChatId(int chatId)
    {
        if (chatId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chatId), "chatIdは1以上である必要があります。");
        }

        return chatId;
    }
}
