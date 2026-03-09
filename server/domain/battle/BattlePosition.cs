using server.domain.battle.enums;

namespace server.domain.battle;

public readonly record struct BattlePosition(BattleRow Row, BattleColumn Column);
