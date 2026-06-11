using server.domain.player;

namespace server.domain.quest;

public interface IQuestRunRepository
{
    Task<QuestRun?> GetAsync(QuestRunId id);
    Task<QuestRun?> GetByRoomIdAsync(QuestRoomId roomId);
    Task<QuestRun?> GetActiveByPlayerAsync(PlayerId playerId);
    Task<bool> ExistsActiveRunByPlayerAsync(PlayerId playerId);
    Task<IReadOnlyList<QuestRun>> ListExpiredAsync(DateTimeOffset now);
    Task SaveAsync(QuestRun run);
    Task SaveChatMessagesAsync(QuestRun run);
}
