namespace server.domain.quest;

public interface IQuestRunRepository
{
    Task<QuestRun?> GetAsync(QuestRunId id);
    Task<QuestRun?> GetByRoomIdAsync(QuestRoomId roomId);
    Task SaveAsync(QuestRun run);
}
