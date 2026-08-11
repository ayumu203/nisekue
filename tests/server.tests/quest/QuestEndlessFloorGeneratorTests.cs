using FluentAssertions;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using server.infrastructure.quest;
using Xunit;

namespace server.tests.quest;

public class QuestEndlessFloorGeneratorTests
{
    private readonly QuestEndlessFloorGenerator generator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 1)]
    [InlineData(11, 2)]
    [InlineData(50, 5)]
    [InlineData(51, 1)] // 50超はテーマ循環
    [InlineData(61, 2)]
    public void ResolveThemeNo_CyclesThroughThemes(int floorNo, int expectedTheme)
    {
        generator.ResolveThemeNo(CreateConfig(), floorNo).Should().Be(expectedTheme);
    }

    [Fact]
    public void GenerateFloor_OnBossInterval_ReturnsSingleScaledBoss()
    {
        var config = CreateConfig();

        var floor = generator.GenerateFloor(config, CreateTemplates(), 10, new Random(1));

        floor.IsBoss.Should().BeTrue();
        floor.Enemies.Should().HaveCount(1);
        var boss = floor.Enemies[0].ScaledDefinition;
        // f(10)=1+0.02*100=3、ボス追加倍率1.5 → base 200 * 3 * 1.5 = 900
        boss.Status.MaxHp.Should().Be(900);
        // 実効Level = round(10 * boss_reward_multiplier 3.0) = 30
        boss.Level.Should().Be(30);
    }

    [Fact]
    public void GenerateFloor_OnNormalFloor_ReturnsNormalEnemiesScaledByGrowthFactor()
    {
        var config = CreateConfig();

        var floor = generator.GenerateFloor(config, CreateTemplates(), 5, new Random(1));

        floor.IsBoss.Should().BeFalse();
        floor.Enemies.Count.Should().BeInRange(1, 3);
        // f(5)=1+0.02*25=1.5 → 通常 base hp 100 * 1.5 = 150、実効Level=floor=5
        floor.Enemies.Should().OnlyContain(x => x.ScaledDefinition.Status.MaxHp == 150);
        floor.Enemies.Should().OnlyContain(x => x.ScaledDefinition.Level == 5);
    }

    [Fact]
    public void GenerateFloor_PicksDistinctTemplatesWithinFloor()
    {
        var config = CreateConfig(minEnemies: 3, maxEnemies: 3);

        var floor = generator.GenerateFloor(config, CreateTemplates(), 5, new Random(7));

        floor.Enemies.Select(x => x.ScaledDefinition.Id.Value).Distinct().Should().HaveCount(3);
    }

    [Fact]
    public void ScaleTemplate_AtExtremeFloor_ClampsToIntMaxWithoutOverflow()
    {
        var config = CreateConfig();
        var template = CreateTemplates().First(x => !x.IsBoss);

        var scaled = generator.ScaleTemplate(config, template, 100_000);

        scaled.Status.MaxHp.Should().Be(int.MaxValue);
        // 攻撃・防御は伸びを抑える指数を掛けるため、HP より遅れて増加する。
        scaled.Status.Strength.Should().BePositive();
        scaled.Status.Defense.Should().BePositive();
    }

    [Fact]
    public async Task CsvEndlessConfigRepository_LoadsEndlessStageConfig()
    {
        var repository = new CsvQuestEndlessConfigRepository();

        var config = await repository.GetByStageIdAsync(new QuestStageId(13));

        config.Should().NotBeNull();
        config!.GrowthCoefficientA.Should().BeApproximately(0.08, 1e-9);
        config.GrowthExponentP.Should().BeApproximately(1.6, 1e-9);
        config.BossInterval.Should().Be(10);
    }

    [Fact]
    public async Task CsvEndlessEnemyTemplateRepository_Loads50Templates()
    {
        var repository = new CsvQuestEndlessEnemyTemplateRepository();

        var templates = await repository.GetAllAsync();

        templates.Should().HaveCount(50);
        // 各テーマ帯にボス1体（計5体）
        templates.Count(x => x.IsBoss).Should().Be(5);
        (await repository.GetByThemeAsync(1)).Should().HaveCount(10);
    }

    [Fact]
    public async Task CsvQuestStageRepository_EndlessStage_HasEndlessConfig()
    {
        var repository = new CsvQuestStageRepository(new CsvQuestEndlessConfigRepository());

        var stage = await repository.GetByStageCodeAsync("endless-abyss");

        stage.Should().NotBeNull();
        stage!.IsEndless.Should().BeTrue();
        stage.ProgressionType.Should().Be(ProgressionType.Endless);
        stage.EndlessConfig.Should().NotBeNull();
        stage.RequiredMapUnlockFlag.Should().Be(MapUnlockFlag.Map3);
    }

    private static QuestEndlessConfig CreateConfig(
        double a = 0.02,
        double p = 2.0,
        int cap = 7161,
        int bossInterval = 10,
        decimal bossExtraMultiplier = 1.5m,
        decimal bossRewardMultiplier = 3.0m,
        int themeCount = 5,
        int floorsPerTheme = 10,
        int minEnemies = 1,
        int maxEnemies = 3,
        decimal expRate = 10000m,
        decimal goldRate = 7500m)
        => new(a, p, cap, bossInterval, bossExtraMultiplier, bossRewardMultiplier,
               themeCount, floorsPerTheme, minEnemies, maxEnemies, expRate, goldRate);

    private static IReadOnlyList<QuestEndlessEnemyTemplate> CreateTemplates()
    {
        var list = new List<QuestEndlessEnemyTemplate>();
        var id = 9001;
        for (var theme = 1; theme <= 5; theme++)
        {
            list.Add(NormalTemplate(id++, theme, EnemyAiType.Aggressive));
            list.Add(NormalTemplate(id++, theme, EnemyAiType.Defensive));
            list.Add(NormalTemplate(id++, theme, EnemyAiType.Supportive));
            list.Add(BossTemplate(id++, theme));
        }

        return list;
    }

    private static QuestEndlessEnemyTemplate NormalTemplate(int id, int theme, EnemyAiType ai)
        => new(
            new QuestEnemyDefinitionId(id),
            theme,
            ai,
            isBoss: false,
            $"通常{id}",
            new Status(100, 10, 20, 15, 12, 10, 10),
            "image/battle/Enemy2.png",
            [new MoveId(127)]);

    private static QuestEndlessEnemyTemplate BossTemplate(int id, int theme)
        => new(
            new QuestEnemyDefinitionId(id),
            theme,
            EnemyAiType.Aggressive,
            isBoss: true,
            $"ボス{id}",
            new Status(200, 20, 40, 25, 20, 15, 12),
            "image/battle/Enemy20.png",
            [new MoveId(129)]);
}
