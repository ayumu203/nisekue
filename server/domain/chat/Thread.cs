using server.domain.player;
using server.shared.constants.chat;

namespace server.domain.chat;

public class Thread
{
    private readonly List<ThreadReply> _replies = [];

    public ThreadId Id { get; }
    public PlayerId AuthorPlayerId { get; }
    public ThreadTitle Title { get; }
    public ThreadBody Body { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastRepliedAt { get; private set; }
    public IReadOnlyList<ThreadReply> Replies => _replies;

    public Thread(
        ThreadId id,
        PlayerId authorPlayerId,
        ThreadTitle title,
        ThreadBody body,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? lastRepliedAt,
        IEnumerable<ThreadReply>? replies = null)
    {
        Id = id;
        AuthorPlayerId = authorPlayerId;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        LastRepliedAt = lastRepliedAt;

        RestoreReplies(replies ?? []);
    }

    public static Thread Create(PlayerId authorPlayerId, string title, string body)
    {
        var now = DateTimeOffset.UtcNow;
        return new Thread(
            ThreadId.New(),
            authorPlayerId,
            new ThreadTitle(title),
            new ThreadBody(body),
            now,
            now,
            null);
    }

    public void AddReply(ThreadReplyId replyId, PlayerId authorPlayerId, ThreadReplyBody body)
    {
        if (_replies.Count >= ThreadConstants.MaxRepliesPerThread)
        {
            throw new InvalidOperationException($"返信は{ThreadConstants.MaxRepliesPerThread}件までです。");
        }

        var now = DateTimeOffset.UtcNow;
        var isAuthorAlerted = authorPlayerId == AuthorPlayerId;
        _replies.Add(new ThreadReply(replyId, authorPlayerId, body, now, isAuthorAlerted));
        UpdatedAt = now;
        LastRepliedAt = now;
    }

    private void RestoreReplies(IEnumerable<ThreadReply> replies)
    {
        _replies.Clear();
        _replies.AddRange(replies.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id.Value));

        if (_replies.Count > ThreadConstants.MaxRepliesPerThread)
        {
            throw new ArgumentException($"返信は{ThreadConstants.MaxRepliesPerThread}件までです。", nameof(replies));
        }
    }
}
