using FluentAssertions;
using server.domain.move;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerTests
{
    [Fact]
    public void Constructor_WhenLevelIsZero_ThrowsArgumentOutOfRangeException()
    {
        var act = () => CreatePlayer(level: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WhenLevelIsPositive_KeepsOriginalValue()
    {
        var player = CreatePlayer(level: 5);

        player.Level.Should().Be(5);
    }

    [Fact]
    public void UpdateImagePath_WhenValidValue_StoresTrimmedPath()
    {
        var player = CreatePlayer(level: 1);

        player.UpdateImagePath("  avatars/player-01.png  ");

        player.ImagePath.Should().Be("avatars/player-01.png");
    }

    [Fact]
    public void SetQuestCooldownUntil_WhenCalled_StoresValue()
    {
        var player = CreatePlayer(level: 1);
        var until = DateTimeOffset.UtcNow.AddMinutes(3);

        player.SetQuestCooldownUntil(until);

        player.QuestCooldownUntil.Should().Be(until);
    }

    [Fact]
    public void LevelUp_WhenExpIsInsufficient_ReturnsNoLevelUp()
    {
        var player = CreatePlayer(level: 1, exp: 9, jobExp: 9, job: Job.Warrior);
        var beforeStatus = player.Status;

        var result = player.LevelUp(CreateJobProfile(Job.Warrior), CreateLearningRule(Job.Warrior));

        result.HasLeveledUp.Should().BeFalse();
        player.Level.Should().Be(1);
        player.JobLevel.Should().Be(1);
        player.Exp.Should().Be(9);
        player.JobExp.Should().Be(9);
        player.Status.Should().Be(beforeStatus);
        player.MoveSet.GetLearnedMoveIds().Should().BeEmpty();
    }

    [Fact]
    public void LevelUp_WhenPlayerAndJobExpReachThreshold_UpdatesBothLevelsAndLearnsMoves()
    {
        var player = CreatePlayer(level: 1, exp: 10, jobExp: 10, job: Job.Warrior);
        var beforeStatus = player.Status;

        var result = player.LevelUp(CreateJobProfile(Job.Warrior, masterLevel: 4), CreateLearningRule(Job.Warrior, 101, 102));

        result.HasPlayerLeveledUp.Should().BeTrue();
        result.HasJobLeveledUp.Should().BeTrue();
        result.HasMasteredCurrentJob.Should().BeFalse();
        result.NewlyLearnedMoveIds.Select(x => x.Id).Should().Equal(101);
        player.Level.Should().Be(2);
        player.JobLevel.Should().Be(2);
        player.Exp.Should().Be(0);
        player.JobExp.Should().Be(0);
        player.Status.MaxHp.Should().Be(beforeStatus.MaxHp + 3);
        player.Status.Strength.Should().Be(beforeStatus.Strength + 2);
        player.Status.Defense.Should().Be(beforeStatus.Defense + 1);
        player.MoveSet.GetLearnedMoveIds().Select(x => x.Id).Should().Equal(101);
    }

    [Fact]
    public void LevelUp_WhenJobReachesMasterLevel_AddsMasteredJobOnlyOnce()
    {
        var player = CreatePlayer(level: 5, exp: 0, jobLevel: 3, jobExp: 30, job: Job.Warrior);

        var first = player.LevelUp(CreateJobProfile(Job.Warrior, masterLevel: 4), CreateLearningRule(Job.Warrior, 101, 102));
        var second = player.LevelUp(CreateJobProfile(Job.Warrior, masterLevel: 4), CreateLearningRule(Job.Warrior, 101, 102));

        first.HasMasteredCurrentJob.Should().BeTrue();
        second.HasMasteredCurrentJob.Should().BeFalse();
        player.MasteredJobs.Should().Contain(Job.Warrior);
    }

    private static Player CreatePlayer(
        int level,
        int exp = 0,
        int jobLevel = 1,
        int jobExp = 0,
        Job job = Job.Apprentice) =>
        new(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: level,
            exp: exp,
            jobLevel: jobLevel,
            jobExp: jobExp,
            status: new Status(
                maxHp: 10,
                maxMp: 0,
                strength: 1,
                defense: 1,
                intelligence: 1,
                luck: 1,
                speed: 1),
            job: job);

    private static JobProfile CreateJobProfile(Job job, int masterLevel = 5)
    {
        return new JobProfile(
            job,
            description: $"{job} profile",
            masterLevel: masterLevel,
            requiredMasterJobs: [],
            growthValue: new GrowthValue(
                MaxHp: 3,
                MaxMp: 0,
                Strength: 2,
                Defense: 1,
                Intelligence: 0,
                Luck: 0,
                Speed: 1));
    }

    private static JobMoveLearningRule CreateLearningRule(Job job, params int[] moveIds)
    {
        return new JobMoveLearningRule(job, moveIds.Select(x => new MoveId(x)).ToArray());
    }
}
