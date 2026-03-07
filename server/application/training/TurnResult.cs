namespace server.application.training;

public sealed record TurnResult(
    int Turn,
    int CurrentPlayerHp,
    int CurrentEnemyHp);
