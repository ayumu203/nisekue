namespace server.domain.quest;

public interface IQuestRoomRepository
{
    Task<QuestRoom?> GetAsync(QuestRoomId id);
    Task SaveAsync(QuestRoom room);
}
