namespace server.application.chat;

public sealed record ThreadDetailView(
    Guid Id,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastRepliedAt,
    string AuthorName,
    string? AuthorImagePath,
    IReadOnlyList<ThreadReplyView> Replies);
