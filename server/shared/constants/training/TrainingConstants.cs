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
        public const int BaseExp = 5;
        public const int LevelBonusBase = 10;
        public const int LevelDiffPenalty = 2;
        public const int LevelBonusMin = 1;
        public const int DamageBonusDivisor = 2;
        public const decimal WinMultiplier = 1.2m;
        public const decimal DrawMultiplier = 1.0m;
        public const decimal LoseMultiplier = 0.8m;
    }
}
