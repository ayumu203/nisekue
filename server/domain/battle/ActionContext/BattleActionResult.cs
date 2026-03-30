using server.domain.battle.enums;
using server.domain.move;

namespace server.domain.battle;

public class BattleActionResult(
    BattleActorId actorId,
    BattleActionKind actionKind,
    MoveId? moveId,
    bool succeeded,
    BattleActionFailureReason? failureReason = null,
    IEnumerable<BattleTargetResult>? targetResults = null,
    bool isTurnEndEffect = false)
{
    private readonly BattleTargetResult[] _targetResults = targetResults?.ToArray() ?? [];

    public BattleActorId ActorId { get; } = actorId;
    public BattleActionKind ActionKind { get; } = actionKind;
    public MoveId? MoveId { get; } = moveId;
    public bool Succeeded { get; } = succeeded;
    public BattleActionFailureReason? FailureReason { get; } = failureReason;
    public IReadOnlyList<BattleTargetResult> TargetResults => _targetResults;
    public bool IsTurnEndEffect { get; } = isTurnEndEffect;
}
