namespace server.domain.quest;

public interface IQuestRunRepository
{
    Task<QuestRun?> GetAsync(QuestRunId id);
    Task<QuestRun?> GetByRoomIdAsync(QuestRoomId roomId);
    Task<IReadOnlyList<QuestRun>> ListExpiredAsync(DateTimeOffset now);
    Task SaveAsync(QuestRun run);
}
