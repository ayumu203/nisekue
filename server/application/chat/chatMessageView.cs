namespace server.application.chat;

public sealed record ChatMessageView(
    int ChatId,
    string SenderName,
    string Message,
    DateTimeOffset CreatedAt);
