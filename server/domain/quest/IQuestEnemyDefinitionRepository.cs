namespace server.domain.quest;

public interface IQuestEnemyDefinitionRepository
{
    Task<QuestEnemyDefinition?> GetAsync(QuestEnemyDefinitionId id);
    Task<IReadOnlyList<QuestEnemyDefinition>> GetAllAsync();
}
