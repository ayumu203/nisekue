using server.domain.battle.enums;
using server.domain.move;

namespace server.domain.battle;

public class BattleAction(
    BattleActorId actorId,
    BattleActionKind kind,
    BattleTargetSelector target,
    MoveId? moveId = null)
{
    public BattleActorId ActorId { get; } = actorId;
    public BattleActionKind Kind { get; } = kind;
    public MoveId? MoveId { get; } = moveId;
    public BattleTargetSelector Target { get; } = target ?? throw new ArgumentNullException(nameof(target));
}
