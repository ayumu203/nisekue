using server.domain.chat;

namespace server.infrastructure.chat;

public class ThreadReplyEntity(
    ThreadReplyId id,
    ThreadId threadId,
    Guid authorPlayerId,
    string body,
    DateTimeOffset createdAt)
{
    public ThreadReplyId Id { get; } = id;
    public ThreadId ThreadId { get; } = threadId;
    public Guid AuthorPlayerId { get; } = authorPlayerId;
    public string Body { get; } = body;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}
