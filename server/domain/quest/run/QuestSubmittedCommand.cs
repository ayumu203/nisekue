using server.domain.battle;
using server.domain.move;
using server.domain.quest.enums;

namespace server.domain.quest;

public class QuestSubmittedCommand(
    QuestParticipantId participantId,
    int turnNo,
    ActionKind actionKind,
    DateTimeOffset submittedAt,
    MoveId? moveId = null,
    BattlePosition? selectedTargetPosition = null,
    bool isAutoSubmitted = false)
{
    public QuestParticipantId ParticipantId { get; } = participantId;
    public int TurnNo { get; } = ValidateTurnNo(turnNo);
    public ActionKind ActionKind { get; } = actionKind;
    public MoveId? MoveId { get; } = moveId;
    public BattlePosition? SelectedTargetPosition { get; } = selectedTargetPosition;
    public DateTimeOffset SubmittedAt { get; } = submittedAt;
    public bool IsAutoSubmitted { get; } = isAutoSubmitted;

    private static int ValidateTurnNo(int turnNo)
    {
        if (turnNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(turnNo), "ターン番号は1以上である必要があります。");
        }

        return turnNo;
    }
}
