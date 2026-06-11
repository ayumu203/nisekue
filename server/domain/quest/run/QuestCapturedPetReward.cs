using server.domain.player;

namespace server.domain.quest;

public sealed record QuestCapturedPetReward(
    PlayerId PlayerId,
    QuestEnemyDefinitionId EnemyDefinitionId,
    DateTimeOffset CapturedAt);
