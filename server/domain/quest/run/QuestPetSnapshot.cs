using server.domain.player;

namespace server.domain.quest;

public class QuestPetSnapshot(QuestEnemyDefinitionId enemyDefinitionId, Status status)
{
    public QuestEnemyDefinitionId EnemyDefinitionId { get; } = enemyDefinitionId;
    public Status Status { get; } = status ?? throw new ArgumentNullException(nameof(status));
}
