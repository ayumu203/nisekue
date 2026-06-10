namespace server.application.chat;

public sealed record GlobalChatRoomView(
    int LastChatId,
    int TotalCount,
    IReadOnlyList<GlobalChatMessageView> Messages);
