using FluentAssertions;
using server.domain.player;
using server.infrastructure.player;
using Xunit;

namespace server.tests;

public class CsvJobRoadmapRankRepositoryTests
{
    [Fact]
    public void GetRank_WhenApprentice_ReturnsZero()
    {
        var repository = new CsvJobRoadmapRankRepository();

        var rank = repository.GetRank(Job.Apprentice);

        rank.Should().Be(0);
    }

    [Fact]
    public void GetRank_WhenBasicJob_ReturnsOne()
    {
        var repository = new CsvJobRoadmapRankRepository();

        var rank = repository.GetRank(Job.Warrior);

        rank.Should().Be(1);
    }

    [Fact]
    public void GetRank_WhenAdvancedJob_ReturnsTwo()
    {
        var repository = new CsvJobRoadmapRankRepository();

        var rank = repository.GetRank(Job.OniWarrior);

        rank.Should().Be(2);
    }

    [Fact]
    public void GetRank_WhenGrandJob_ReturnsThree()
    {
        var repository = new CsvJobRoadmapRankRepository();

        var rank = repository.GetRank(Job.GrandWarrior);

        rank.Should().Be(3);
    }

    [Fact]
    public void GetRank_WhenRank4Job_ReturnsFour()
    {
        var repository = new CsvJobRoadmapRankRepository();

        var rank = repository.GetRank(Job.Shogun);

        rank.Should().Be(4);
    }

    [Fact]
    public void GetRank_WhenRank5Job_ReturnsFive()
    {
        var repository = new CsvJobRoadmapRankRepository();

        var rank = repository.GetRank(Job.Bushin);

        rank.Should().Be(5);
    }

    [Fact]
    public void GetRank_WhenShugoshin_ReturnsFive()
    {
        var repository = new CsvJobRoadmapRankRepository();

        var rank = repository.GetRank(Job.Shugoshin);

        rank.Should().Be(5);
    }

    [Fact]
    public void GetJobs_ReturnsAllJobsInOrder()
    {
        var repository = new CsvJobRoadmapRankRepository();

        var jobs = repository.GetJobs();

        jobs.Should().HaveCount(29);
        jobs[0].Should().Be(Job.Apprentice);
    }
}
