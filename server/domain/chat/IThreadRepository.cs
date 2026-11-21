using server.domain.player;

namespace server.domain.chat;

public interface IThreadRepository
{
    /// <summary>スレッドを作成する。作成上限を超えて自動削除したスレッドがあれば、その画像キーを返す。</summary>
    Task<IReadOnlyList<string>> CreateAsync(Thread thread, int maxThreadCount);
    Task<Thread?> FindByIdAsync(ThreadId threadId);
    Task SaveAsync(Thread thread, int maxReplyCount);
    Task<ThreadPageResult> GetPageAsync(ThreadQuery query);

    /// <summary>スレッドを削除し、付いていた画像のキーを返す（画像がなければ null）。</summary>
    Task<string?> DeleteAsync(ThreadId threadId);

    /// <summary>画像のセンシティブ設定を変更する。スレッドがないか画像が付いていなければ false。</summary>
    Task<bool> SetImageSensitiveAsync(ThreadId threadId, bool isSensitive);

    Task<IReadOnlyList<ThreadAlertSummary>> GetAlertSummariesAsync(PlayerId authorPlayerId);
    Task<int> MarkRepliesAlertedAsync(PlayerId authorPlayerId, IReadOnlyCollection<ThreadReplyId> replyIds);
}
