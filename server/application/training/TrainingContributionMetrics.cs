namespace server.application.training;

public sealed record TrainingContributionMetrics(
    int PlayerDealtTotalDamage,
    int PlayerEffectiveHealTotal,
    int CurrentPlayerHp);
