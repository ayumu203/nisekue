using server.domain.player;

namespace server.application.chat;

public sealed record ChatRoomView(
    PlayerId OwnerId,
    int LastChatId,
    IReadOnlyList<ChatMessageView> Messages);
