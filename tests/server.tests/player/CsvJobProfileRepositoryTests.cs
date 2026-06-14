using FluentAssertions;
using server.domain.player;
using server.infrastructure.player;
using Xunit;

namespace server.tests;

public class CsvJobProfileRepositoryTests
{
    [Fact]
    public void GetByJob_WhenSecondTierJobExists_ReturnsExpectedRequirements()
    {
        var repository = new CsvJobProfileRepository();

        var profile = repository.GetByJob(Job.OniWarrior);

        profile.Job.Should().Be(Job.OniWarrior);
        profile.MasterLevel.Should().Be(30);
        profile.RequiredMasterJobs.Should().BeEquivalentTo([Job.Warrior]);
        profile.GrowthValue.Strength.Should().Be(6);
    }

    [Fact]
    public void GetByJob_WhenFourthTierJobHasThreeRequirements_ReturnsAllRequirements()
    {
        var repository = new CsvJobProfileRepository();

        var profile = repository.GetByJob(Job.GreatThief);

        profile.Job.Should().Be(Job.GreatThief);
        profile.MasterLevel.Should().Be(50);
        profile.RequiredMasterJobs.Should().BeEquivalentTo([Job.Warrior, Job.WindMage, Job.GrandRanger]);
        profile.GrowthValue.Luck.Should().Be(8);
    }

    [Fact]
    public void GetByJob_WhenTankFourthTierJobExists_ReturnsExpectedRequirements()
    {
        var repository = new CsvJobProfileRepository();

        var profile = repository.GetByJob(Job.GreatKnight);

        profile.Job.Should().Be(Job.GreatKnight);
        profile.MasterLevel.Should().Be(50);
        profile.RequiredMasterJobs.Should().BeEquivalentTo([Job.GrandGuard]);
        profile.GrowthValue.Defense.Should().Be(10);
    }

    [Fact]
    public void GetByJob_WhenShugoshinExists_ReturnsGreatKnightRequirement()
    {
        var repository = new CsvJobProfileRepository();

        var profile = repository.GetByJob(Job.Shugoshin);

        profile.Job.Should().Be(Job.Shugoshin);
        profile.MasterLevel.Should().Be(60);
        profile.RequiredMasterJobs.Should().BeEquivalentTo([Job.GreatKnight]);
        profile.GrowthValue.Defense.Should().Be(12);
    }
}
