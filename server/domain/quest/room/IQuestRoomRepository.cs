using server.domain.player;

namespace server.domain.quest;

public interface IQuestRoomRepository
{
    Task<QuestRoom?> GetAsync(QuestRoomId id);
    Task<QuestRoom?> GetRecruitingByOwnerAsync(PlayerId ownerId);
    Task<IReadOnlyList<QuestRoom>> SearchAsync(QuestRoomSearchCondition condition);
    Task SaveAsync(QuestRoom room);
}
