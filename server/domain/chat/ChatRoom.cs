using server.domain.player;

namespace server.domain.chat;

public class ChatRoom(PlayerId ownerId, int lastChatId = 0)
{
    public PlayerId OwnerId { get; } = ownerId;
    public int LastChatId { get; private set; } = ValidateLastChatId(lastChatId);
    private readonly List<ChatMessage> _messages = [];
    public IReadOnlyList<ChatMessage> Messages => _messages;

    public int GetNextMessageId()
    {
        checked
        {
            return LastChatId + 1;
        }
    }

    public void PostMessage(PlayerId senderId, string text)
    {
        var chatId = GetNextMessageId();
        ChatText body = new(text);
        var now = DateTimeOffset.UtcNow;
        ChatMessage chatMessage = new(senderId, chatId, body, now);
        _messages.Add(chatMessage);
        LastChatId = chatId;
        EnforceMessageLimit();
    }

    public void EnforceMessageLimit()
    {
        if (_messages.Count <= 50) return;

        _messages.RemoveRange(0, _messages.Count - 50);
    }

    private static int ValidateLastChatId(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "lastChatIdに負数は設定できません。");
        }

        return value;
    }
}
