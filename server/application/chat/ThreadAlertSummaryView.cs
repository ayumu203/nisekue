namespace server.application.chat;

public sealed record ThreadAlertSummaryView(
    Guid ThreadId,
    string ThreadTitle,
    IReadOnlyList<Guid> ReplyIds,
    string LatestReplyAuthorName,
    DateTimeOffset LatestReplyCreatedAt,
    int UnalertedReplyCount);
