using server.domain.battle;
using server.domain.battle.enums;
using server.domain.player;

namespace server.domain.quest;

/// <summary>
/// エンドレスの 1 フロアを通算フロア番号 n から動的生成するドメインサービス。
/// 敵ステータス = テンプレ基準 S_base × f(n)(=1+a·n^p) を int 範囲にクランプして適用する。
/// フロア50超はテーマ循環、boss_interval ごとにボスフロア（追加倍率・報酬補正）。
/// </summary>
public class QuestEndlessFloorGenerator
{
    /// <summary>防御・知力の伸びを抑える指数。1.0 で HP と同率。</summary>
    private const double DefenseFalloffExponent = 0.45d;

    /// <summary>攻撃・運・速さの伸びを抑える指数。1.0 で HP と同率。</summary>
    private const double OffenseFalloffExponent = 0.6d;

    // 通常フロア(最大3体)の配置スロット。
    private static readonly BattlePosition[] NormalPositions =
    [
        new(BattleRow.Front, BattleColumn.Left),
        new(BattleRow.Middle, BattleColumn.Right),
        new(BattleRow.Back, BattleColumn.Left),
    ];

    private static readonly BattlePosition BossPosition = new(BattleRow.Front, BattleColumn.Left);

    public QuestEndlessFloor GenerateFloor(
        QuestEndlessConfig config,
        IReadOnlyList<QuestEndlessEnemyTemplate> templates,
        int floorNo,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(random);
        if (floorNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(floorNo), "フロア番号は1以上である必要があります。");
        }

        var themeNo = ResolveThemeNo(config, floorNo);
        var themeTemplates = templates.Where(x => x.ThemeNo == themeNo).ToList();
        if (themeTemplates.Count == 0)
        {
            throw new InvalidOperationException($"テーマ {themeNo} の敵テンプレが存在しません。floorNo={floorNo}");
        }

        if (config.IsBossFloor(floorNo))
        {
            var boss = themeTemplates.FirstOrDefault(x => x.IsBoss)
                ?? throw new InvalidOperationException($"テーマ {themeNo} のボステンプレが存在しません。");
            var enemy = BuildEnemy(config, boss, floorNo, 1, BossPosition);
            return new QuestEndlessFloor(floorNo, themeNo, true, [enemy]);
        }

        var normals = themeTemplates.Where(x => !x.IsBoss).ToList();
        if (normals.Count == 0)
        {
            throw new InvalidOperationException($"テーマ {themeNo} の通常敵テンプレが存在しません。");
        }

        var maxByPool = Math.Min(normals.Count, NormalPositions.Length);
        var minCount = Math.Clamp(config.MinEnemiesPerFloor, 1, maxByPool);
        var maxCount = Math.Clamp(config.MaxEnemiesPerFloor, minCount, maxByPool);
        var count = random.Next(minCount, maxCount + 1);

        var picked = Shuffle(normals, random).Take(count).ToList();
        var enemies = new List<QuestEndlessFloorEnemy>(count);
        for (var index = 0; index < picked.Count; index++)
        {
            enemies.Add(BuildEnemy(config, picked[index], floorNo, index + 1, NormalPositions[index]));
        }

        return new QuestEndlessFloor(floorNo, themeNo, false, enemies);
    }

    /// <summary>
    /// テンプレ1体を通算フロア番号 floorNo にスケールした敵定義へ変換する。
    /// ボステンプレは追加倍率（ステ）と報酬補正（実効Level）を適用する。
    /// 戦闘中フロアの敵定義を再構築する用途でも使う。
    /// </summary>
    public QuestEnemyDefinition ScaleTemplate(QuestEndlessConfig config, QuestEndlessEnemyTemplate template, int floorNo)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(template);
        if (floorNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(floorNo), "フロア番号は1以上である必要があります。");
        }

        var factor = config.GrowthFactor(floorNo) * (template.IsBoss ? (double)config.BossExtraMultiplier : 1.0);
        var level = template.IsBoss ? EffectiveLevel(floorNo, config.BossRewardMultiplier) : floorNo;
        return new QuestEnemyDefinition(
            template.Id,
            template.Name,
            level,
            ScaleStatus(template.BaseStatus, factor),
            template.ImagePath,
            template.Archetype,
            template.MoveIds);
    }

    /// <summary>通算フロア番号からテーマ番号(1始まり)を求める。floors_per_theme×theme_count 超は循環。</summary>
    public int ResolveThemeNo(QuestEndlessConfig config, int floorNo)
    {
        ArgumentNullException.ThrowIfNull(config);
        var band = (floorNo - 1) / config.FloorsPerTheme;
        return (band % config.ThemeCount) + 1;
    }

    private QuestEndlessFloorEnemy BuildEnemy(
        QuestEndlessConfig config,
        QuestEndlessEnemyTemplate template,
        int floorNo,
        int placementNo,
        BattlePosition position)
    {
        var definition = ScaleTemplate(config, template, floorNo);
        var placement = new QuestEnemyPlacement(placementNo, template.Id, position);
        return new QuestEndlessFloorEnemy(placement, definition);
    }

    // 報酬用の実効 Level（深さに対し線形）。ボスは追加倍率を乗じる。
    private static int EffectiveLevel(int floorNo, decimal multiplier)
    {
        var value = (decimal)floorNo * multiplier;
        return Math.Max(1, (int)Math.Round(value, MidpointRounding.AwayFromZero));
    }

    // S_base の戦闘7ステのみ f(n) でスケール。命中/回避/会心/軽減はテンプレ値を維持。
    // 攻撃・防御は HP と同率で伸ばさない。ダメージ計算が減算式のため、同率だと
    // 「敵防御がプレイヤー筋力を超えてダメージが最低保証の1に固定される壁」と
    // 「敵攻撃がプレイヤー防御を超えた瞬間の即死」が同時に発生し、
    // 一定フロアから先が緩やかな難化ではなく到達不能になってしまう。
    private static Status ScaleStatus(Status baseStatus, double factor)
    {
        var defenseFactor = Math.Pow(factor, DefenseFalloffExponent);
        var offenseFactor = Math.Pow(factor, OffenseFalloffExponent);
        return new Status(
            ScaleStat(baseStatus.MaxHp, factor),
            ScaleStat(baseStatus.MaxMp, factor),
            ScaleStat(baseStatus.Strength, offenseFactor),
            ScaleStat(baseStatus.Defense, defenseFactor),
            // 知力は魔法防御としても働くため防御側の指数を使う。
            ScaleStat(baseStatus.Intelligence, defenseFactor),
            ScaleStat(baseStatus.Luck, offenseFactor),
            ScaleStat(baseStatus.Speed, offenseFactor),
            baseStatus.Accuracy,
            baseStatus.Evasion,
            baseStatus.CriticalChance,
            baseStatus.DamageReduction);
    }

    private static int ScaleStat(int baseValue, double factor)
    {
        var scaled = baseValue * factor;
        if (scaled >= int.MaxValue)
        {
            return int.MaxValue;
        }

        var rounded = (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
        return Math.Max(1, rounded);
    }

    private static List<T> Shuffle<T>(IReadOnlyList<T> source, Random random)
    {
        var list = source.ToList();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}

public sealed record QuestEndlessFloorEnemy(QuestEnemyPlacement Placement, QuestEnemyDefinition ScaledDefinition);

public sealed record QuestEndlessFloor(int FloorNo, int ThemeNo, bool IsBoss, IReadOnlyList<QuestEndlessFloorEnemy> Enemies);
