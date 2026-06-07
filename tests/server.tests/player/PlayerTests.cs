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
    public void LevelUp_WhenPlayerExpReachThreshold_UpdatesBothLevelsAndLearnsMoves()
    {
        var player = CreatePlayer(level: 1, exp: 10, jobExp: 9, job: Job.Warrior);
        var beforeStatus = player.Status;

        var result = player.LevelUp(CreateJobProfile(Job.Warrior, masterLevel: 4), CreateLearningRule(Job.Warrior, 101, 102));

        result.HasPlayerLeveledUp.Should().BeTrue();
        result.HasJobLeveledUp.Should().BeFalse();
        result.HasMasteredCurrentJob.Should().BeFalse();
        player.Level.Should().Be(2);
        player.JobLevel.Should().Be(1);
        player.Exp.Should().Be(0);
        player.JobExp.Should().Be(9);
        player.Status.MaxHp.Should().Be(beforeStatus.MaxHp + 3);
        player.Status.Strength.Should().Be(beforeStatus.Strength + 2);
        player.Status.Defense.Should().Be(beforeStatus.Defense + 1);
    }

    [Fact]
    public void LevelUp_WhenPlayerJobExpReachThreshold_UpdatesBothLevelsAndLearnsMoves()
    {
        var player = CreatePlayer(level: 1, exp: 9, jobExp: 10, job: Job.Warrior);
        var beforeStatus = player.Status;

        var result = player.LevelUp(CreateJobProfile(Job.Warrior, masterLevel: 4), CreateLearningRule(Job.Warrior, 101, 102));

        result.HasPlayerLeveledUp.Should().BeFalse();
        result.HasJobLeveledUp.Should().BeTrue();
        result.HasMasteredCurrentJob.Should().BeFalse();
        result.NewlyLearnedMoveIds.Select(x => x.Id).Should().Equal(101);
        player.Level.Should().Be(1);
        player.JobLevel.Should().Be(2);
        player.Exp.Should().Be(9);
        player.JobExp.Should().Be(0);
        player.Status.MaxHp.Should().Be(beforeStatus.MaxHp);
        player.Status.Strength.Should().Be(beforeStatus.Strength);
        player.Status.Defense.Should().Be(beforeStatus.Defense);
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

    [Fact]
    public void GainGold_WhenPositiveValue_AddsGold()
    {
        var player = CreatePlayer(level: 1);

        player.GainGold(25);

        player.Gold.Should().Be(125);
    }

    [Fact]
    public void SpendGold_WhenEnoughGold_DecreasesGold()
    {
        var player = CreatePlayer(level: 1);

        player.SpendGold(40);

        player.Gold.Should().Be(60);
    }

    [Fact]
    public void SpendGold_WhenGoldIsInsufficient_Throws()
    {
        var player = CreatePlayer(level: 1);

        var act = () => player.SpendGold(101);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*所持 Gold が不足しています*");
    }

    [Fact]
    public void Rebirth_WhenLevelIsBelowRequirement_Throws()
    {
        var player = CreatePlayer(level: 99, gold: 100000);

        var act = () => player.Rebirth(CreateInheritedStatus());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*転生にはレベル100以上が必要です*");
    }

    [Fact]
    public void Rebirth_WhenGoldIsBelowRequirement_Throws()
    {
        var player = CreatePlayer(level: 100, gold: 99999);

        var act = () => player.Rebirth(CreateInheritedStatus());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*転生には100000 Goldが必要です*");
    }

    [Fact]
    public void Rebirth_WhenEligible_ResetsLevelsAndKeepsJobMovesAndMasteredJobs()
    {
        var moveSet = new MoveSet();
        moveSet.SetSlot(0, new MoveId(421));
        moveSet.SetSlot(1, new MoveId(141));
        var player = CreatePlayer(
            level: 120,
            exp: 45,
            jobLevel: 9,
            jobExp: 27,
            gold: 150000,
            job: Job.Mage,
            status: new Status(120, 80, 30, 28, 42, 18, 25),
            moveSet: moveSet,
            masteredJobs: new HashSet<Job> { Job.Warrior, Job.Priest });

        player.Rebirth(new Status(35, 24, 9, 8, 12, 5, 7));

        player.Job.Should().Be(Job.Mage);
        player.Level.Should().Be(1);
        player.Exp.Should().Be(0);
        player.JobLevel.Should().Be(1);
        player.JobExp.Should().Be(0);
        player.Gold.Should().Be(50000);
        player.Status.MaxHp.Should().Be(35);
        player.Status.MaxMp.Should().Be(24);
        player.Status.Strength.Should().Be(9);
        player.MoveSet.GetLearnedMoveIds().Select(x => x.Id).Should().Equal(421, 141);
        player.MasteredJobs.Should().BeEquivalentTo(new[] { Job.Warrior, Job.Priest });
    }

    [Fact]
    public void SetExpMultiplierFlag_WhenFlagIsNotSet_SetsFlag()
    {
        var player = CreatePlayer(level: 1);

        player.SetExpMultiplierFlag(0x8);

        player.ExpMultiplierFlags.Should().Be(0x8);
    }

    [Fact]
    public void SetExpMultiplierFlag_WhenSameFlagAlreadySet_Throws()
    {
        var player = CreatePlayer(level: 1);
        player.SetExpMultiplierFlag(0x4);

        var act = () => player.SetExpMultiplierFlag(0x4);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*すでに経験値倍率が設定されています*");
    }

    [Fact]
    public void SetExpMultiplierFlag_WhenDifferentFlagAlreadySet_Throws()
    {
        var player = CreatePlayer(level: 1);
        player.SetExpMultiplierFlag(0x4);

        var act = () => player.SetExpMultiplierFlag(0x8);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*すでに経験値倍率が設定されています*");
    }

    [Fact]
    public void HasAnyExpMultiplierFlag_WhenFlagIsSet_ReturnsTrue()
    {
        var player = CreatePlayer(level: 1);
        player.SetExpMultiplierFlag(0x1);

        player.HasAnyExpMultiplierFlag().Should().BeTrue();
    }

    [Fact]
    public void HasAnyExpMultiplierFlag_WhenFlagIsZero_ReturnsFalse()
    {
        var player = CreatePlayer(level: 1);

        player.HasAnyExpMultiplierFlag().Should().BeFalse();
    }

    [Fact]
    public void GainExp_WhenCalled_AddsExpDirectly()
    {
        var player = CreatePlayer(level: 1, exp: 0, jobExp: 0);

        player.GainExp(100);

        player.Exp.Should().Be(100);
        player.JobExp.Should().Be(100);
    }

    [Fact]
    public void GainExp_WhenFlagIsSet_DoesNotAffectExpAddition()
    {
        var player = CreatePlayer(level: 1, exp: 0, jobExp: 0);
        player.SetExpMultiplierFlag(0x8);

        player.GainExp(100);

        player.Exp.Should().Be(100);
        player.JobExp.Should().Be(100);
    }

    [Fact]
    public void ClearExpMultiplierFlags_WhenCalled_ResetsFlagsToZero()
    {
        var player = CreatePlayer(level: 1);
        player.SetExpMultiplierFlag(0x8);

        player.ClearExpMultiplierFlags();

        player.ExpMultiplierFlags.Should().Be(0);
        player.HasAnyExpMultiplierFlag().Should().BeFalse();
    }

    [Fact]
    public void ExpMultiplierFlags_ToFlag_When1_1x_Returns0x8()
    {
        ExpMultiplierFlag.ToFlag(1.1m).Should().Be(0x8);
    }

    [Fact]
    public void ExpMultiplierFlags_ToFlag_When1_5x_Returns0x4()
    {
        ExpMultiplierFlag.ToFlag(1.5m).Should().Be(0x4);
    }

    [Fact]
    public void ExpMultiplierFlags_ToFlag_When2_0x_Returns0x2()
    {
        ExpMultiplierFlag.ToFlag(2.0m).Should().Be(0x2);
    }

    [Fact]
    public void ExpMultiplierFlags_ToFlag_When3_0x_Returns0x1()
    {
        ExpMultiplierFlag.ToFlag(3.0m).Should().Be(0x1);
    }

    [Fact]
    public void ExpMultiplierFlags_ToFlag_WhenUnknownMultiplier_Throws()
    {
        var act = () => ExpMultiplierFlag.ToFlag(5.0m);
        act.Should().Throw<ArgumentException>().WithMessage("*未対応の経験値倍率*");
    }

    [Fact]
    public void ExpMultiplierFlags_ToMultiplier_WhenFlagsAreZero_Returns1_0x()
    {
        ExpMultiplierFlag.ToMultiplier(0).Should().Be(1.0m);
    }

    [Fact]
    public void ExpMultiplierFlags_ToMultiplier_When0x8_Returns1_1x()
    {
        ExpMultiplierFlag.ToMultiplier(0x8).Should().Be(1.1m);
    }

    [Fact]
    public void ExpMultiplierFlags_ToMultiplier_When0x4_Returns1_5x()
    {
        ExpMultiplierFlag.ToMultiplier(0x4).Should().Be(1.5m);
    }

    [Fact]
    public void ExpMultiplierFlags_ToMultiplier_When0x2_Returns2_0x()
    {
        ExpMultiplierFlag.ToMultiplier(0x2).Should().Be(2.0m);
    }

    [Fact]
    public void ExpMultiplierFlags_ToMultiplier_When0x1_Returns3_0x()
    {
        ExpMultiplierFlag.ToMultiplier(0x1).Should().Be(3.0m);
    }

    private static Player CreatePlayer(
        int level,
        int exp = 0,
        int jobLevel = 1,
        int jobExp = 0,
        int gold = 100,
        Job job = Job.Apprentice,
        Status? status = null,
        MoveSet? moveSet = null,
        IReadOnlySet<Job>? masteredJobs = null) =>
        new(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: level,
            exp: exp,
            jobLevel: jobLevel,
            jobExp: jobExp,
            gold: gold,
            status: status ?? new Status(
                maxHp: 10,
                maxMp: 0,
                strength: 1,
                defense: 1,
                intelligence: 1,
                luck: 1,
                speed: 1),
            job: job,
            moveSet: moveSet,
            masteredJobs: masteredJobs);

    private static Status CreateInheritedStatus()
    {
        return new Status(
            maxHp: 10,
            maxMp: 3,
            strength: 2,
            defense: 2,
            intelligence: 2,
            luck: 1,
            speed: 1);
    }

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

    [Fact]
    public void IsRoadmapUnlocked_WhenDefaultConstructed_ApprenticeIsUnlocked()
    {
        var player = CreatePlayer(level: 1);

        player.IsRoadmapUnlocked(Job.Apprentice).Should().BeTrue();
    }

    [Fact]
    public void IsRoadmapUnlocked_WhenDefaultConstructed_WarriorIsNotUnlocked()
    {
        var player = CreatePlayer(level: 1);

        player.IsRoadmapUnlocked(Job.Warrior).Should().BeFalse();
    }

    [Fact]
    public void UnlockRoadmap_WhenCalled_IsRoadmapUnlockedReturnsTrue()
    {
        var player = CreatePlayer(level: 1);

        player.UnlockRoadmap(Job.Warrior);

        player.IsRoadmapUnlocked(Job.Warrior).Should().BeTrue();
    }

    [Fact]
    public void RoadmapUnlockFlags_WhenMultipleUnlocked_DoesNotAffectEachOther()
    {
        var player = CreatePlayer(level: 1);

        player.UnlockRoadmap(Job.Warrior);
        player.UnlockRoadmap(Job.Mage);

        player.IsRoadmapUnlocked(Job.Warrior).Should().BeTrue();
        player.IsRoadmapUnlocked(Job.Mage).Should().BeTrue();
        player.IsRoadmapUnlocked(Job.Guardian).Should().BeFalse();
    }
}
