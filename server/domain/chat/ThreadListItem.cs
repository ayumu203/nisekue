using server.domain.player;

namespace server.domain.chat;

public sealed record ThreadListItem(
    ThreadId Id,
    PlayerId AuthorPlayerId,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastRepliedAt,
    int ReplyCount);
