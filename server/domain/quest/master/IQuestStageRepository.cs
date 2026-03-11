namespace server.domain.quest;

public interface IQuestStageRepository
{
    Task<QuestStageDefinition?> GetAsync(QuestStageId id);
    Task<QuestStageDefinition?> GetByStageCodeAsync(string stageCode);
    Task<IReadOnlyList<QuestStageDefinition>> GetAllAsync();
}
