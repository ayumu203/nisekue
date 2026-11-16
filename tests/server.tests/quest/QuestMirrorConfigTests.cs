using FluentAssertions;
using server.domain.quest;
using server.infrastructure.quest;
using Xunit;

namespace server.tests.quest;

public class QuestMirrorConfigTests
{
    [Fact]
    public void Constructor_WithInitialValues_KeepsValues()
    {
        var config = MirrorTestConfigs.Default();

        config.TopPlayerCount.Should().Be(20);
        config.EnemiesPerNormalFloor.Should().Be(5);
        config.BossCompanionCount.Should().Be(3);
        config.MaxNormalAppearances.Should().Be(2);
        config.MaxTotalAppearances.Should().Be(3);
        config.SecondChoiceRate.Should().Be(0.15);
        config.SelfBuffRate.Should().Be(0.5);
        config.SelfHarmMinHpPercent.Should().Be(20);
        config.HealHpPercent.Should().Be(50);
        config.RestoreMpPercent.Should().Be(30);
        config.MaxRevivesPerFloor.Should().Be(3);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Constructor_WhenRateIsOutOfRange_Throws(double rate)
    {
        var act = () => MirrorTestConfigs.Default(selfBuffRate: rate);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("selfBuffRate");
    }

    [Fact]
    public void Constructor_WhenEnemiesPerNormalFloorExceedsSlots_Throws()
    {
        var act = () => MirrorTestConfigs.Default(enemiesPerNormalFloor: QuestMirrorConfig.MaxEnemiesPerFloor + 1);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("enemiesPerNormalFloor");
    }

    [Fact]
    public void Constructor_WhenBossFloorWouldExceedSlots_Throws()
    {
        var act = () => MirrorTestConfigs.Default(bossCompanionCount: QuestMirrorConfig.MaxEnemiesPerFloor);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("bossCompanionCount");
    }

    [Fact]
    public void Constructor_WhenTotalLimitIsBelowNormalLimit_Throws()
    {
        var act = () => MirrorTestConfigs.Default(maxNormalAppearances: 3, maxTotalAppearances: 2);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxTotalAppearances");
    }

    [Fact]
    public async Task GetByStageIdAsync_WithFixtureCsv_ReturnsMirrorStageConfig()
    {
        var repository = new CsvQuestMirrorConfigRepository();

        var config = await repository.GetByStageIdAsync(new QuestStageId(14));

        config.Should().NotBeNull();
        config!.TopPlayerCount.Should().Be(20);
        config.SelfBuffRate.Should().Be(0.5);
        config.MaxRevivesPerFloor.Should().Be(3);
    }

    [Fact]
    public async Task GetByStageCodeAsync_WhenMirrorStage_HasMirrorConfig()
    {
        var repository = new CsvQuestStageRepository(new CsvQuestEndlessConfigRepository(), new CsvQuestMirrorConfigRepository());

        var mirror = await repository.GetByStageCodeAsync("meikan-shrine");
        var normal = await repository.GetByStageCodeAsync("trial-forest");

        mirror!.MirrorConfig.Should().NotBeNull();
        normal!.MirrorConfig.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenMirrorStageHasNoConfig_Throws()
    {
        var act = () => new QuestStageDefinition(
            new QuestStageId(14),
            "meikan-shrine",
            "名鑑の祠",
            "image/quest/mirror-shrine-battlefield.svg",
            100,
            minimumEntryLevel: 100,
            1,
            4,
            [],
            equipmentRewards: [],
            itemRewards: [],
            isActive: true,
            progressionType: server.domain.quest.enums.ProgressionType.Mirror);

        act.Should().Throw<ArgumentNullException>().WithParameterName("mirrorConfig");
    }
}
