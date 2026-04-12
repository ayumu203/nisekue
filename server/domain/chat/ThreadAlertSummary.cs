using server.domain.player;

namespace server.domain.chat;

public sealed record ThreadAlertSummary(
    ThreadId ThreadId,
    string ThreadTitle,
    IReadOnlyList<ThreadReplyId> ReplyIds,
    PlayerId LatestReplyAuthorPlayerId,
    DateTimeOffset LatestReplyCreatedAt,
    int UnalertedReplyCount);
