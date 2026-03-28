using server.domain.player;

namespace server.application.chat;

public sealed record ChatMessageView(
    int ChatId,
    string SenderType,
    PlayerId? SenderId,
    string SenderName,
    string? ImagePath,
    string Message,
    DateTimeOffset CreatedAt);
