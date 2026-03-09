using server.domain.battle.enums;
using server.domain.move.enums;

namespace server.application.battle;

public record BattleActionInput(
    Guid ActorId,
    BattleActionKind Kind,
    int? MoveId,
    TargetType TargetType,
    AttackRange AttackRange,
    IReadOnlyList<Guid>? TargetActorIds = null);
