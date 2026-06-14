using FluentAssertions;
using server.domain.quest;
using server.infrastructure.quest;
using Xunit;

namespace server.tests.quest;

public class CsvQuestResourceRepositoryTests
{
    [Fact]
    public async Task GetByStageCodeAsync_WhenTrialForestExists_ReturnsExpectedStageDefinition()
    {
        var repository = new CsvQuestStageRepository();

        var stage = await repository.GetByStageCodeAsync("trial-forest");

        stage.Should().NotBeNull();
        stage!.Id.Value.Should().Be(11);
        stage.Name.Should().Be("試練の森");
        stage.RecommendedLevel.Should().Be(1000);
        stage.MinimumEntryLevel.Should().Be(600);
        stage.Floors.Should().HaveCount(7);
        stage.Floors.Single(x => x.FloorNo == 7).Placements.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetByStageCodeAsync_WhenVoidArmoryExists_ReturnsExpectedStageDefinition()
    {
        var repository = new CsvQuestStageRepository();

        var stage = await repository.GetByStageCodeAsync("void-armory");

        stage.Should().NotBeNull();
        stage!.Id.Value.Should().Be(8);
        stage.Name.Should().Be("はがねの墓場");
        stage.RecommendedLevel.Should().Be(2000);
        stage.MinimumEntryLevel.Should().Be(1000);
        stage.Floors.Should().HaveCount(10);
        stage.Floors.Single(x => x.FloorNo == 10).Placements.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAsync_WhenTrialForestEnemiesExist_ReturnsExpectedImagePaths()
    {
        var repository = new CsvQuestEnemyDefinitionRepository();

        var fairy = await repository.GetAsync(new QuestEnemyDefinitionId(77));
        var forestGod = await repository.GetAsync(new QuestEnemyDefinitionId(81));

        fairy.Should().NotBeNull();
        fairy!.Name.Should().Be("花守のフェアリー");
        fairy.ImagePath.Should().Be("image/battle/Enemy123.png");
        forestGod.Should().NotBeNull();
        forestGod!.Name.Should().Be("試練の森神");
        forestGod.ImagePath.Should().Be("image/battle/Enemy127.png");
    }

    [Fact]
    public async Task GetAsync_WhenNewKnightAndMachineEnemiesExist_ReturnsExpectedImagePaths()
    {
        var repository = new CsvQuestEnemyDefinitionRepository();

        var knight = await repository.GetAsync(new QuestEnemyDefinitionId(59));
        var machine = await repository.GetAsync(new QuestEnemyDefinitionId(60));

        knight.Should().NotBeNull();
        knight!.Name.Should().Be("黒盾の騎士");
        knight.ImagePath.Should().Be("image/battle/Enemy111.png");
        machine.Should().NotBeNull();
        machine!.Name.Should().Be("はがねの番兵");
        machine.ImagePath.Should().Be("image/battle/Enemy116.png");
    }

    [Fact]
    public async Task GetForStartAsync_WhenTrialForestRequiresNpcFill_ReturnsHighLevelTemplates()
    {
        var repository = new CsvQuestNpcTemplateRepository();

        var templates = await repository.GetForStartAsync(new QuestStageId(11), 4);

        templates.Should().HaveCount(4);
        templates.Should().OnlyContain(x => x.Level == 1000);
        templates.Select(x => x.Id.Value).Should().OnlyContain(id => id >= 45 && id <= 49);
    }

    [Fact]
    public async Task GetForStartAsync_WhenVoidArmoryRequiresNpcFill_ReturnsHighLevelTemplates()
    {
        var repository = new CsvQuestNpcTemplateRepository();

        var templates = await repository.GetForStartAsync(new QuestStageId(8), 4);

        templates.Should().HaveCount(4);
        templates.Should().OnlyContain(x => x.Level == 2000);
        templates.Select(x => x.Id.Value).Should().OnlyContain(id => id >= 30 && id <= 34);
    }
}
