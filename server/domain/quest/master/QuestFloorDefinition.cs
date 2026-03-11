using server.domain.quest.enums;

namespace server.domain.quest;

public class QuestFloorDefinition(
    int floorNo,
    FloorType floorType,
    IEnumerable<QuestEnemyPlacement> placements,
    QuestFloorRewardRule rewardRule)
{
    private readonly QuestEnemyPlacement[] placements = placements?.ToArray()
        ?? throw new ArgumentNullException(nameof(placements));

    public int FloorNo { get; } = ValidateFloorNo(floorNo);
    public FloorType FloorType { get; } = floorType;
    public IReadOnlyList<QuestEnemyPlacement> Placements => placements;
    public QuestFloorRewardRule RewardRule { get; } = rewardRule;

    private static int ValidateFloorNo(int floorNo)
    {
        if (floorNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(floorNo), "階層番号は1以上である必要があります。");
        }

        return floorNo;
    }
}
