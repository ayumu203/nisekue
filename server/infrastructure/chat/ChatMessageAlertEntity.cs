using server.domain.player;

namespace server.infrastructure.chat;

public class ChatMessageAlertEntity(PlayerId ownerId, int chatId, bool isAlerted)
{
    public PlayerId OwnerId { get; } = ownerId;
    public int ChatId { get; } = chatId;
    public bool IsAlerted { get; set; } = isAlerted;
}
