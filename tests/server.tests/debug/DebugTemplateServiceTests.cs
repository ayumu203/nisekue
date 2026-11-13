using FluentAssertions;
using server.application.debug;
using server.domain.player;
using Xunit;

namespace server.tests;

public class DebugTemplateServiceTests
{
    [Fact]
    public void GetTemplates_ReturnsInitialAndFirstTierJobsOnly()
    {
        var service = CreateService(DebugTestPlayers.Create());

        var templates = service.GetTemplates();

        templates.Select(x => x.Id).Should().Equal("initial", "master-Warrior", "master-Mage");
    }

    [Fact]
    public async Task ApplyAsync_Initial_ResetsProgress()
    {
        var player = DebugTestPlayers.Create(level: 40, job: Job.Warrior, moveIds: [201, 202]);
        player.MarkJobAsMastered(Job.Warrior);
        player.UnlockRoadmap(Job.Mage);
        var service = CreateService(player);

        var result = await service.ApplyAsync(player.Id, "initial");

        result.Level.Should().Be(1);
        player.Level.Should().Be(1);
        player.Exp.Should().Be(0);
        player.Job.Should().Be(Job.Apprentice);
        player.JobLevel.Should().Be(1);
        player.MasteredJobs.Should().BeEmpty();
        player.IsRoadmapUnlocked(Job.Mage).Should().BeFalse();
        player.Status.MaxHp.Should().Be(PlayerInitialState.CreateStatus().MaxHp);
        player.MoveSet.Slots.Select(x => x?.Id).Should().Equal(101, null, null, null, null, null, null, null, null, null);
        player.Gold.Should().Be(100);
    }

    [Fact]
    public async Task ApplyAsync_FirstTierMastered_FollowsNormalPath()
    {
        var player = DebugTestPlayers.Create(level: 60, job: Job.Mage);
        var service = CreateService(player);

        var result = await service.ApplyAsync(player.Id, "master-Warrior");

        // 見習い: Lv1→5 で職業レベル5（マスター）。戦士: 職業レベル1→25 で Lv5→29
        result.Job.Should().Be(Job.Warrior);
        result.Level.Should().Be(29);
        player.JobLevel.Should().Be(25);
        player.MasteredJobs.Should().BeEquivalentTo(new[] { Job.Apprentice, Job.Warrior });
        player.Status.MaxHp.Should().BeGreaterThan(PlayerInitialState.CreateStatus().MaxHp);
    }

    [Fact]
    public async Task ApplyAsync_WhenUnknownTemplate_Throws()
    {
        var player = DebugTestPlayers.Create();
        var service = CreateService(player);

        var act = () => service.ApplyAsync(player.Id, "master-SwordMaster");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static DebugTemplateService CreateService(Player player)
    {
        return new DebugTemplateService(
            new SinglePlayerMutationService(player),
            new TieredJobProfileRepository(),
            new FixedJobMoveLearningRuleRepository());
    }

    /// <summary>見習い（マスター5）、一次職の戦士・魔法使い（25）、二次職の剣聖（戦士が前提）。</summary>
    private sealed class TieredJobProfileRepository : IJobProfileRepository
    {
        private static readonly Job[] Jobs = [Job.Apprentice, Job.Warrior, Job.Mage, Job.SwordMaster];

        public JobProfile GetByJob(Job job)
        {
            return new JobProfile(
                job,
                description: $"{job} profile",
                masterLevel: job switch
                {
                    Job.Apprentice => 5,
                    Job.SwordMaster => 35,
                    _ => 25,
                },
                requiredMasterJobs: job == Job.SwordMaster ? [Job.Warrior] : [],
                growthValue: new GrowthValue(MaxHp: 2, MaxMp: 1, Strength: 1, Defense: 1, Intelligence: 1, Luck: 1, Speed: 1));
        }

        public IReadOnlyList<JobProfile> GetAll() => Jobs.Select(GetByJob).ToArray();
    }
}
