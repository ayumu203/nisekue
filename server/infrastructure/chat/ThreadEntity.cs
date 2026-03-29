using server.domain.chat;

namespace server.infrastructure.chat;

public class ThreadEntity(
    ThreadId id,
    Guid authorPlayerId,
    string title,
    string body,
    DateTimeOffset createdAt,
    DateTimeOffset updatedAt,
    DateTimeOffset? lastRepliedAt)
{
    public ThreadId Id { get; } = id;
    public Guid AuthorPlayerId { get; } = authorPlayerId;
    public string Title { get; } = title;
    public string Body { get; } = body;
    public DateTimeOffset CreatedAt { get; } = createdAt;
    public DateTimeOffset UpdatedAt { get; } = updatedAt;
    public DateTimeOffset? LastRepliedAt { get; } = lastRepliedAt;
}
