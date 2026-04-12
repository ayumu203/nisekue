using server.domain.player;

namespace server.domain.chat;

public interface IThreadRepository
{
    Task CreateAsync(Thread thread, int maxThreadCount);
    Task<Thread?> FindByIdAsync(ThreadId threadId);
    Task SaveAsync(Thread thread, int maxReplyCount);
    Task<ThreadPageResult> GetPageAsync(ThreadQuery query);
    Task DeleteAsync(ThreadId threadId);
    Task<IReadOnlyList<ThreadAlertSummary>> GetAlertSummariesAsync(PlayerId authorPlayerId);
    Task<int> MarkRepliesAlertedAsync(PlayerId authorPlayerId, IReadOnlyCollection<ThreadReplyId> replyIds);
}
