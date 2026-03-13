using server.domain.move;

namespace server.domain.quest;

public class QuestTrapState(
    QuestTrapId id,
    QuestParticipantId sourceParticipantId,
    MoveId moveId,
    int expiresAfterFloorNo,
    bool isTriggered = false)
{
    public QuestTrapId Id { get; } = id;
    public QuestParticipantId SourceParticipantId { get; } = sourceParticipantId;
    public MoveId MoveId { get; } = moveId ?? throw new ArgumentNullException(nameof(moveId));
    public int ExpiresAfterFloorNo { get; } = ValidateFloorNo(expiresAfterFloorNo);
    public bool IsTriggered { get; private set; } = isTriggered;

    public void MarkTriggered()
    {
        IsTriggered = true;
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
