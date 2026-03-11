namespace server.application.training;

public sealed record TrainingBattleSummary(
    int Turn,
    int CurrentPlayerHp,
    int CurrentEnemyHp,
    TrainingOutcome Outcome);
