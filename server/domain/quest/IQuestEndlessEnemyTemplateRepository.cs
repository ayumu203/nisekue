namespace server.domain.quest;

public interface IQuestEndlessEnemyTemplateRepository
{
    Task<QuestEndlessEnemyTemplate?> GetAsync(QuestEnemyDefinitionId id);
    Task<IReadOnlyList<QuestEndlessEnemyTemplate>> GetAllAsync();

    /// <summary>指定テーマ帯のテンプレ（通常敵 / ボス）を取得。</summary>
    Task<IReadOnlyList<QuestEndlessEnemyTemplate>> GetByThemeAsync(int themeNo);
}
