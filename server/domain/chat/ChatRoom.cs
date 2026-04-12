using server.domain.player;

namespace server.domain.chat;

public class ChatRoom(PlayerId ownerId, int lastChatId = 0)
{
    public PlayerId OwnerId { get; } = ownerId;
    public int LastChatId { get; private set; } = ValidateLastChatId(lastChatId);
    private readonly List<ChatMessage> _messages = [];
    public IReadOnlyList<ChatMessage> Messages => _messages;

    public ChatRoom(PlayerId ownerId, int lastChatId, IEnumerable<ChatMessage> messages)
        : this(ownerId, lastChatId)
    {
        RestoreMessages(messages);
    }

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
        var isAlerted = senderId == OwnerId;
        ChatMessage chatMessage = new(ChatMessageSenderType.Player, senderId, chatId, body, now, isAlerted);
        _messages.Add(chatMessage);
        LastChatId = chatId;
        EnforceMessageLimit();
    }

    public void PostSystemMessage(string text)
    {
        var chatId = GetNextMessageId();
        ChatText body = new(text);
        var now = DateTimeOffset.UtcNow;
        ChatMessage chatMessage = new(ChatMessageSenderType.System, senderId: null, chatId, body, now, isAlerted: false);
        _messages.Add(chatMessage);
        LastChatId = chatId;
        EnforceMessageLimit();
    }

    public void EnforceMessageLimit()
    {
        if (_messages.Count <= 50) return;

        _messages.RemoveRange(0, _messages.Count - 50);
    }

    public void RestoreMessages(IEnumerable<ChatMessage> messages)
    {
        if (messages is null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        _messages.Clear();
        _messages.AddRange(messages.OrderBy(x => x.ChatId));
        EnforceMessageLimit();

        var maxChatId = _messages.Count == 0 ? 0 : _messages[^1].ChatId;
        LastChatId = Math.Max(LastChatId, maxChatId);
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
