using server.domain.player;

namespace server.domain.chat;

public class ChatMessage(PlayerId senderId, int chatId, ChatText body, DateTimeOffset createdAt)
{
    public PlayerId SenderId { get; } = senderId;
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
