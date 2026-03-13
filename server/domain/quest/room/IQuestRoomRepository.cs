namespace server.domain.quest;

public interface IQuestRoomRepository
{
    Task<QuestRoom?> GetAsync(QuestRoomId id);
    Task<IReadOnlyList<QuestRoom>> SearchAsync(QuestRoomSearchCondition condition);
    Task SaveAsync(QuestRoom room);
}
