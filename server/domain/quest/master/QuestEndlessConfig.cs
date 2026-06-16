namespace server.domain.quest;

/// <summary>
/// エンドレスステージのスケーリング設定（endless_configs.csv 由来）。
/// 敵ステータス = アーキタイプ基準 S_base × f(n) (= 1 + a·n^p)。
/// </summary>
public class QuestEndlessConfig(
    double growthCoefficientA,
    double growthExponentP,
    int safetyCapFloor,
    int bossInterval,
    decimal bossExtraMultiplier,
    decimal bossRewardMultiplier,
    int themeCount,
    int floorsPerTheme,
    int minEnemiesPerFloor,
    int maxEnemiesPerFloor,
    decimal rewardExpRate,
    decimal rewardGoldRate)
{
    public double GrowthCoefficientA { get; } = ValidatePositive(growthCoefficientA, nameof(growthCoefficientA));
    public double GrowthExponentP { get; } = ValidateGreaterThanOne(growthExponentP, nameof(growthExponentP));
    public int SafetyCapFloor { get; } = ValidatePositiveInt(safetyCapFloor, nameof(safetyCapFloor));
    public int BossInterval { get; } = ValidatePositiveInt(bossInterval, nameof(bossInterval));
    public decimal BossExtraMultiplier { get; } = ValidatePositiveDecimal(bossExtraMultiplier, nameof(bossExtraMultiplier));
    public decimal BossRewardMultiplier { get; } = ValidatePositiveDecimal(bossRewardMultiplier, nameof(bossRewardMultiplier));
    public int ThemeCount { get; } = ValidatePositiveInt(themeCount, nameof(themeCount));
    public int FloorsPerTheme { get; } = ValidatePositiveInt(floorsPerTheme, nameof(floorsPerTheme));
    public int MinEnemiesPerFloor { get; } = ValidatePositiveInt(minEnemiesPerFloor, nameof(minEnemiesPerFloor));
    public int MaxEnemiesPerFloor { get; } = ValidateMaxEnemies(maxEnemiesPerFloor, minEnemiesPerFloor);
    public decimal RewardExpRate { get; } = ValidateNonNegativeDecimal(rewardExpRate, nameof(rewardExpRate));
    public decimal RewardGoldRate { get; } = ValidateNonNegativeDecimal(rewardGoldRate, nameof(rewardGoldRate));

    /// <summary>成長倍率 f(n) = 1 + a·n^p。</summary>
    public double GrowthFactor(int floorNo)
    {
        return 1.0 + (GrowthCoefficientA * Math.Pow(floorNo, GrowthExponentP));
    }

    /// <summary>通算フロア番号がボスフロア（boss_interval の倍数）か。</summary>
    public bool IsBossFloor(int floorNo) => floorNo > 0 && floorNo % BossInterval == 0;

    private static double ValidatePositive(double value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "0より大きい必要があります。");
        }

        return value;
    }

    private static double ValidateGreaterThanOne(double value, string paramName)
    {
        if (value <= 1.0)
        {
            throw new ArgumentOutOfRangeException(paramName, "1より大きい必要があります（超線形成長のため）。");
        }

        return value;
    }

    private static int ValidatePositiveInt(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "1以上である必要があります。");
        }

        return value;
    }

    private static decimal ValidatePositiveDecimal(decimal value, string paramName)
    {
        if (value <= 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, "0より大きい必要があります。");
        }

        return value;
    }

    private static decimal ValidateNonNegativeDecimal(decimal value, string paramName)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, "0以上である必要があります。");
        }

        return value;
    }

    private static int ValidateMaxEnemies(int maxValue, int minValue)
    {
        if (maxValue < minValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValue), "max_enemies_per_floor は min_enemies_per_floor 以上である必要があります。");
        }

        return maxValue;
    }
}
