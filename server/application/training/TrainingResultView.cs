namespace server.application.training;

public sealed record TrainingResultView(
    string TrainingResult,
    int Turn,
    int CurrentPlayerHp,
    int MaxPlayerHp,
    int CurrentEnemyHp,
    int MaxEnemyHp,
    int Exp,
    bool IsPlayerLevelUp,
    bool IsJobLevelUp,
    int WeaponMasteryDelta,
    IReadOnlyList<server.application.player.LearnedMoveView> NewlyLearnedMoves);
