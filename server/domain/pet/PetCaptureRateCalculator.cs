namespace server.domain.pet;

public static class PetCaptureRateCalculator
{
    public const int MinRatePercent = 5;
    public const int MaxRatePercent = 95;
    public const int LevelDiffRatePercent = 5;

    public static int Calculate(int playerLevel, int enemyLevel)
    {
        if (playerLevel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playerLevel), "レベルは1以上である必要があります。");
        }

        if (enemyLevel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(enemyLevel), "レベルは1以上である必要があります。");
        }

        var rate = GetBaseRatePercent(enemyLevel) + (playerLevel - enemyLevel) * LevelDiffRatePercent;
        return Math.Clamp(rate, MinRatePercent, MaxRatePercent);
    }

    public static int GetBaseRatePercent(int enemyLevel)
    {
        return enemyLevel switch
        {
            < 10 => 75,
            < 20 => 60,
            < 40 => 50,
            < 60 => 40,
            < 100 => 30,
            < 200 => 20,
            < 400 => 15,
            < 700 => 10,
            _ => 5
        };
    }
}
