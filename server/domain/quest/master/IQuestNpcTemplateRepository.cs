namespace server.domain.quest;

public interface IQuestNpcTemplateRepository
{
    Task<IReadOnlyList<QuestNpcTemplate>> GetForStartAsync(QuestStageId stageId, int count);
    Task<IReadOnlyList<QuestNpcTemplate>> GetAllAsync();
}
