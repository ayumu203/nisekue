using server.domain.battle;
using server.domain.battle.enums;
using server.domain.player;

namespace server.application.battle;

public record BattleActorInput(
    Guid ActorId,
    string DisplayName,
    BattleSide Side,
    Status BaseStatus,
    MoveSet MoveSet,
    int? CurrentHp = null,
    int? CurrentMp = null,
    IReadOnlyList<BattleAilmentState>? Ailments = null,
    IReadOnlyList<BattleBuffState>? Buffs = null);
