namespace server.domain.quest;

/// <summary>
/// 名鑑の祠の調整値（mirror_configs.csv 由来）。敵の選出と行動で使う。
/// docs/quest/06_名鑑の祠設計.md の「7.1 調整値」。
/// </summary>
public class QuestMirrorConfig
{
    /// <summary>1フロアに置ける敵の数（3列×2）。</summary>
    public const int MaxEnemiesPerFloor = 6;

    public QuestMirrorConfig(
        int topPlayerCount,
        int enemiesPerNormalFloor,
        int bossCompanionCount,
        int maxNormalAppearances,
        int maxTotalAppearances,
        double secondChoiceRate,
        double selfBuffRate,
        int selfHarmMinHpPercent,
        int healHpPercent,
        int restoreMpPercent,
        int maxRevivesPerFloor)
    {
        TopPlayerCount = ValidateRange(topPlayerCount, 1, int.MaxValue, nameof(topPlayerCount));
        EnemiesPerNormalFloor = ValidateRange(enemiesPerNormalFloor, 1, MaxEnemiesPerFloor, nameof(enemiesPerNormalFloor));
        // ボスフロアは自分＋同行者なので、同行者は1フロアの上限−1まで。
        BossCompanionCount = ValidateRange(bossCompanionCount, 0, MaxEnemiesPerFloor - 1, nameof(bossCompanionCount));
        MaxNormalAppearances = ValidateRange(maxNormalAppearances, 1, int.MaxValue, nameof(maxNormalAppearances));
        MaxTotalAppearances = ValidateRange(maxTotalAppearances, maxNormalAppearances, int.MaxValue, nameof(maxTotalAppearances));
        SecondChoiceRate = ValidateRate(secondChoiceRate, nameof(secondChoiceRate));
        SelfBuffRate = ValidateRate(selfBuffRate, nameof(selfBuffRate));
        SelfHarmMinHpPercent = ValidateRange(selfHarmMinHpPercent, 0, 100, nameof(selfHarmMinHpPercent));
        HealHpPercent = ValidateRange(healHpPercent, 0, 100, nameof(healHpPercent));
        RestoreMpPercent = ValidateRange(restoreMpPercent, 0, 100, nameof(restoreMpPercent));
        MaxRevivesPerFloor = ValidateRange(maxRevivesPerFloor, 0, int.MaxValue, nameof(maxRevivesPerFloor));
    }

    /// <summary>敵の候補にする上位の人数。</summary>
    public int TopPlayerCount { get; }

    /// <summary>通常フロア1つあたりの敵の数。</summary>
    public int EnemiesPerNormalFloor { get; }

    /// <summary>ボスフロアで自分と一緒に出る上位の人数。</summary>
    public int BossCompanionCount { get; }

    /// <summary>1人が通常フロアに出る回数の上限。</summary>
    public int MaxNormalAppearances { get; }

    /// <summary>1人がボスフロアを含めて出る回数の上限。</summary>
    public int MaxTotalAppearances { get; }

    /// <summary>第2候補の行動を選ぶ確率（0〜1）。</summary>
    public double SecondChoiceRate { get; }

    /// <summary>戦士系・魔法系が自分へのバフを使う確率（0〜1）。</summary>
    public double SelfBuffRate { get; }

    /// <summary>自傷バフを使える HP の下限（%）。</summary>
    public int SelfHarmMinHpPercent { get; }

    /// <summary>この HP（%）以下の味方がいれば回復を使う。</summary>
    public int HealHpPercent { get; }

    /// <summary>この MP（%）以下なら MP 回復を使う。</summary>
    public int RestoreMpPercent { get; }

    /// <summary>1フロアで敵1体が蘇生できる回数。</summary>
    public int MaxRevivesPerFloor { get; }

    private static int ValidateRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"{min}〜{max} の範囲である必要があります。");
        }

        return value;
    }

    private static double ValidateRate(double value, string paramName)
    {
        if (double.IsNaN(value) || value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "0〜1 の範囲である必要があります。");
        }

        return value;
    }
}
