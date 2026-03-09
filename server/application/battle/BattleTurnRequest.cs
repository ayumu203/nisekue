using server.domain.move;

namespace server.application.battle;

public record BattleTurnRequest(
    IReadOnlyList<BattleActorInput> Actors,
    IReadOnlyList<BattleActionInput> Actions,
    IReadOnlyList<Move> Moves);
