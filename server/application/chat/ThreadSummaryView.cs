namespace server.application.chat;

public sealed record ThreadSummaryView(
    Guid Id,
    string Title,
    string PreviewBody,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastRepliedAt,
    int ReplyCount,
    string AuthorName,
    string? AuthorImagePath);
