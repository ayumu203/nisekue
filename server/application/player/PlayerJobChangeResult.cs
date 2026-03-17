namespace server.application.player;

public sealed record PlayerJobChangeResult(
    server.domain.player.Player Player,
    IReadOnlyList<LearnedMoveView> NewlyLearnedMoves);
