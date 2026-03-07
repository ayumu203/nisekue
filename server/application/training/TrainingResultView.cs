namespace server.application.training;

public sealed record TrainingResultView(
    string TrainingResult,
    int Turn,
    int CurrentPlayerHp,
    int MaxPlayerHp,
    int CurrentEnemyHp,
    int MaxEnemyHp,
    int Exp,
    bool IsLevelUp);
