using server.domain.player;

namespace server.domain.chat;

public class ThreadReply(
    ThreadReplyId id,
    PlayerId authorPlayerId,
    ThreadReplyBody body,
    DateTimeOffset createdAt,
    bool isAuthorAlerted)
{
    public ThreadReplyId Id { get; } = id;
    public PlayerId AuthorPlayerId { get; } = authorPlayerId;
    public ThreadReplyBody Body { get; } = body ?? throw new ArgumentNullException(nameof(body));
    public DateTimeOffset CreatedAt { get; } = createdAt;
    public bool IsAuthorAlerted { get; } = isAuthorAlerted;
}
