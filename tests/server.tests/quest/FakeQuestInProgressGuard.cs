using server.application.quest;
using server.domain.player;
using server.domain.quest;

namespace server.tests.quest;

/// <summary>
/// 本物の <see cref="QuestInProgressGuard"/> に「進行中ランの有無」だけを返すリポジトリを渡したもの。
/// </summary>
internal sealed class FakeQuestInProgressGuard(bool isInQuest = false) : IQuestInProgressGuard
{
    private readonly QuestInProgressGuard inner = new(new StubQuestRunRepository(isInQuest));

    public Task<bool> IsInQuestAsync(PlayerId playerId) => inner.IsInQuestAsync(playerId);

    public Task EnsureNotInQuestAsync(PlayerId playerId, string message) => inner.EnsureNotInQuestAsync(playerId, message);

    private sealed class StubQuestRunRepository(bool hasActiveRun) : IQuestRunRepository
    {
        public Task<bool> ExistsActiveRunByPlayerAsync(PlayerId playerId) => Task.FromResult(hasActiveRun);

        public Task<QuestRun?> GetAsync(QuestRunId id) => throw new NotSupportedException();
        public Task<QuestRun?> GetForResolutionAsync(QuestRunId id) => throw new NotSupportedException();
        public Task<QuestRoomId?> GetRoomIdAsync(QuestRunId id) => throw new NotSupportedException();
        public Task<QuestRun?> GetByRoomIdAsync(QuestRoomId roomId) => throw new NotSupportedException();
        public Task<QuestRun?> GetActiveByPlayerAsync(PlayerId playerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<QuestRun>> ListExpiredAsync(DateTimeOffset now) => throw new NotSupportedException();
        public Task SaveAsync(QuestRun run) => throw new NotSupportedException();
        public Task SaveChatMessagesAsync(QuestRun run) => throw new NotSupportedException();
    }
}
