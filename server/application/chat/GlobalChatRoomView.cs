namespace server.application.chat;

public sealed record GlobalChatRoomView(
    int LastChatId,
    IReadOnlyList<GlobalChatMessageView> Messages);
