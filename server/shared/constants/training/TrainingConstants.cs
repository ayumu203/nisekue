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
    }

    public static class Exp
    {
        public const int DamageBaseDivisor = 10;
        public const int BaseExpMin = 1;
        public const int LevelBonusHighThreshold = 5;
        public const int LevelBonusMidThreshold = 10;
        public const decimal LevelBonusHigh = 0.5m;
        public const decimal LevelBonusMid = 0.2m;
        public const decimal LevelBonusLow = 0.0m;
        public const decimal WinMultiplier = 1.2m;
        public const decimal DrawMultiplier = 1.0m;
        public const decimal LoseMultiplier = 0.8m;
    }
}
