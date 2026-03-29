namespace server.application.chat;

public sealed record ThreadReplyView(
    Guid Id,
    string Body,
    DateTimeOffset CreatedAt,
    string AuthorName,
    string? AuthorImagePath);
