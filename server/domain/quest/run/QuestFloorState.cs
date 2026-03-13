namespace server.domain.quest;

public class QuestFloorState(int currentFloorNo, bool isBossFloor, IEnumerable<QuestEnemyPlacement>? currentPlacements = null)
{
    private QuestEnemyPlacement[] currentPlacements = currentPlacements?.ToArray() ?? [];

    public int CurrentFloorNo { get; private set; } = ValidateFloorNo(currentFloorNo);
    public bool IsBossFloor { get; private set; } = isBossFloor;
    public IReadOnlyList<QuestEnemyPlacement> CurrentPlacements => currentPlacements;

    public void AdvanceTo(int floorNo, bool isBossFloor, IEnumerable<QuestEnemyPlacement> placements)
    {
        CurrentFloorNo = ValidateFloorNo(floorNo);
        IsBossFloor = isBossFloor;
        currentPlacements = placements?.ToArray() ?? throw new ArgumentNullException(nameof(placements));
    }

    private static int ValidateFloorNo(int floorNo)
    {
        if (floorNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(floorNo), "階層番号は1以上である必要があります。");
        }

        return floorNo;
    }
}
