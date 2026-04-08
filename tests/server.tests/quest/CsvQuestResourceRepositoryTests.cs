using FluentAssertions;
using server.domain.quest;
using server.infrastructure.quest;
using Xunit;

namespace server.tests.quest;

public class CsvQuestResourceRepositoryTests
{
    [Fact]
    public async Task GetByStageCodeAsync_WhenVoidArmoryExists_ReturnsExpectedStageDefinition()
    {
        var repository = new CsvQuestStageRepository();

        var stage = await repository.GetByStageCodeAsync("void-armory");

        stage.Should().NotBeNull();
        stage!.Id.Value.Should().Be(8);
        stage.Name.Should().Be("虚鉄の兵装墓所");
        stage.RecommendedLevel.Should().Be(2000);
        stage.MinimumEntryLevel.Should().Be(1000);
        stage.Floors.Should().HaveCount(5);
        stage.Floors.Single(x => x.FloorNo == 5).Placements.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAsync_WhenNewKnightAndMachineEnemiesExist_ReturnsExpectedImagePaths()
    {
        var repository = new CsvQuestEnemyDefinitionRepository();

        var knight = await repository.GetAsync(new QuestEnemyDefinitionId(59));
        var machine = await repository.GetAsync(new QuestEnemyDefinitionId(60));

        knight.Should().NotBeNull();
        knight!.Name.Should().Be("断罪の黒盾騎士");
        knight.ImagePath.Should().Be("image/battle/Enemy111.png");
        machine.Should().NotBeNull();
        machine!.Name.Should().Be("奈落機兵グレイブ");
        machine.ImagePath.Should().Be("image/battle/Enemy116.png");
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
