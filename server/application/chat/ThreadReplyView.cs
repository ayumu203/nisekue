namespace server.application.chat;

public sealed record ThreadReplyView(
    Guid Id,
    string Body,
    DateTimeOffset CreatedAt,
    Guid AuthorPlayerId,
    string AuthorName,
    string? AuthorImagePath);
