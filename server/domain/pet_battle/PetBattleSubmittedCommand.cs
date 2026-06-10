using server.domain.battle;
using server.domain.move;
using server.domain.quest.enums;

namespace server.domain.pet_battle;

public class PetBattleSubmittedCommand(
    PetBattleParticipantId participantId,
    int turnNo,
    ActionKind actionKind,
    DateTimeOffset submittedAt,
    MoveId? moveId = null,
    BattlePosition? selectedTargetPosition = null,
    bool isAutoSubmitted = false)
{
    public PetBattleParticipantId ParticipantId { get; } = participantId;
    public int TurnNo { get; } = turnNo > 0 ? turnNo : throw new ArgumentOutOfRangeException(nameof(turnNo));
    public ActionKind ActionKind { get; } = actionKind;
    public MoveId? MoveId { get; } = moveId;
    public BattlePosition? SelectedTargetPosition { get; } = selectedTargetPosition;
    public DateTimeOffset SubmittedAt { get; } = submittedAt;
    public bool IsAutoSubmitted { get; } = isAutoSubmitted;
}
