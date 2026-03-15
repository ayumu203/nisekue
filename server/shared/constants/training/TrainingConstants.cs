namespace server.shared.constants.training;

public static class TrainingConstants
{
    public static class Constraints
    {
        public const int EnemyNameMaxLength = 20;
        public const int EnemyImagePathMaxLength = 200;
    }

    public static class Battle
    {
        public const int MaxTurns = 3;
        public const int CooldownSeconds = 3;
    }

    public static class Exp
    {
        public const int MinimumExp = 1;
        public const int BaseExpPerEnemyLevel = 3;
        public const decimal BaseContribution = 0.2m;
        public const decimal AttackContributionWeight = 0.5m;
        public const decimal HealContributionWeight = 0.15m;
        public const decimal SurvivalContributionWeight = 0.15m;
        public const int LevelRateVeryLowThreshold = -6;
        public const int LevelRateLowThreshold = -3;
        public const int LevelRateNormalThreshold = 2;
        public const int LevelRateHighThreshold = 5;
        public const decimal LevelRateVeryLow = 0.3m;
        public const decimal LevelRateLow = 0.6m;
        public const decimal LevelRateNormal = 1.0m;
        public const decimal LevelRateHigh = 1.3m;
        public const decimal LevelRateVeryHigh = 1.5m;
        public const decimal WinMultiplier = 1.0m;
        public const decimal DrawMultiplier = 0.85m;
        public const decimal LoseMultiplier = 0.7m;
    }
}
