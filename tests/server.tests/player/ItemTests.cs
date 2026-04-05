using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests;

public class ItemTests
{
    [Fact]
    public void CanUse_WhenPlayerLevelIsBelowRequiredLevel_ReturnsFalse()
    {
        var item = new Item(
            new ItemId(3002),
            "戦士の証",
            "戦士へ転職する証。",
            maxStack: 99,
            effectType: ItemEffectType.ChangeJob,
            changeJobTo: Job.Warrior,
            requiredLevel: 10);
        var player = CreatePlayer(level: 9);

        item.CanUse(player).Should().BeFalse();
    }

    [Fact]
    public void CanUse_WhenRequiredMasterJobsAreNotSatisfied_ReturnsFalse()
    {
        var item = new Item(
            new ItemId(3002),
            "上級職の証",
            "熟練者のみが扱える証。",
            maxStack: 99,
            effectType: ItemEffectType.ChangeJob,
            changeJobTo: Job.Warrior,
            requiredMasterJobs: new HashSet<Job> { Job.Priest });
        var player = CreatePlayer(level: 20, masteredJobs: new HashSet<Job> { Job.Warrior });

        item.CanUse(player).Should().BeFalse();
    }

    [Fact]
    public void CanUse_WhenConditionsAreSatisfied_ReturnsTrue()
    {
        var item = new Item(
            new ItemId(3002),
            "上級職の証",
            "熟練者のみが扱える証。",
            maxStack: 99,
            effectType: ItemEffectType.ChangeJob,
            changeJobTo: Job.Warrior,
            requiredLevel: 10,
            requiredMasterJobs: new HashSet<Job> { Job.Priest });
        var player = CreatePlayer(level: 20, masteredJobs: new HashSet<Job> { Job.Priest });

        item.CanUse(player).Should().BeTrue();
    }

    [Fact]
    public void CanUse_WhenRebirthedAndMasterRequirementsSatisfied_IgnoresRequiredLevelForChangeJob()
    {
        var item = new Item(
            new ItemId(3065),
            "大魔導の証",
            "グランドキャスターへの転職証。",
            maxStack: 99,
            effectType: ItemEffectType.ChangeJob,
            changeJobTo: Job.GrandCaster,
            requiredLevel: 40,
            requiredMasterJobs: new HashSet<Job> { Job.FireMage, Job.WaterMage, Job.WindMage });
        var player = CreatePlayer(
            level: 1,
            rebirthCount: 1,
            masteredJobs: new HashSet<Job> { Job.FireMage, Job.WaterMage, Job.WindMage });

        item.CanUse(player).Should().BeTrue();
    }

    [Fact]
    public void CanUse_WhenRebirthedButMasterRequirementsMissing_DoesNotIgnoreRequiredLevelForChangeJob()
    {
        var item = new Item(
            new ItemId(3065),
            "大魔導の証",
            "グランドキャスターへの転職証。",
            maxStack: 99,
            effectType: ItemEffectType.ChangeJob,
            changeJobTo: Job.GrandCaster,
            requiredLevel: 40,
            requiredMasterJobs: new HashSet<Job> { Job.FireMage, Job.WaterMage, Job.WindMage });
        var player = CreatePlayer(
            level: 1,
            rebirthCount: 1,
            masteredJobs: new HashSet<Job> { Job.FireMage, Job.WaterMage });

        item.CanUse(player).Should().BeFalse();
    }

    private static Player CreatePlayer(
        int level,
        IReadOnlySet<Job>? masteredJobs = null,
        int rebirthCount = 0)
    {
        return new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: level,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(10, 10, 10, 10, 10, 10, 10),
            job: Job.Apprentice,
            moveSet: new MoveSet(),
            masteredJobs: masteredJobs,
            rebirthCount: rebirthCount);
    }
}
