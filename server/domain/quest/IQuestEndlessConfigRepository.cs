namespace server.domain.quest;

public interface IQuestEndlessConfigRepository
{
    Task<QuestEndlessConfig?> GetByStageIdAsync(QuestStageId stageId);
}
