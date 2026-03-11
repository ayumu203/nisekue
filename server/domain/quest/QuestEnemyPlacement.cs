using server.domain.battle;

namespace server.domain.quest;

public class QuestEnemyPlacement(int placementNo, QuestEnemyDefinitionId enemyDefinitionId, BattlePosition position)
{
    public int PlacementNo { get; } = ValidatePlacementNo(placementNo);
    public QuestEnemyDefinitionId EnemyDefinitionId { get; } = enemyDefinitionId;
    public BattlePosition Position { get; } = position;

    private static int ValidatePlacementNo(int placementNo)
    {
        if (placementNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placementNo), "配置番号は1以上である必要があります。");
        }

        return placementNo;
    }
}
