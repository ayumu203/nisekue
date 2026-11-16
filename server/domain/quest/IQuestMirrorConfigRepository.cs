namespace server.domain.quest;

public interface IQuestMirrorConfigRepository
{
    Task<QuestMirrorConfig?> GetByStageIdAsync(QuestStageId stageId);
}
