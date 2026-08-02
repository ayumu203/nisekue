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
            new ItemId(9999),
            "鬼武者の証",
            "鬼武者へ転職する証。",
            maxStack: 99,
            effectType: ItemEffectType.ChangeJob,
            changeJobTo: Job.OniWarrior,
            requiredLevel: 10);
        var player = CreatePlayer(level: 9);

        item.CanUse(player).Should().BeFalse();
    }

    [Fact]
    public void CanUse_WhenRequiredMasterJobsAreNotSatisfied_ReturnsFalse()
    {
        var item = new Item(
            new ItemId(9999),
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
            new ItemId(9999),
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
    public void Constructor_WhenExpMultiplierItem_CreatesItemSuccessfully()
    {
        var item = new Item(
            new ItemId(3071),
            "経験の秘石",
            "経験値が1.1倍になる",
            maxStack: 99,
            effectType: ItemEffectType.ExpMultiplier,
            expMultiplier: 1.1m);

        item.EffectType.Should().Be(ItemEffectType.ExpMultiplier);
        item.ExpMultiplier.Should().Be(1.1m);
    }

    private static Player CreatePlayer(int level, IReadOnlySet<Job>? masteredJobs = null)
    {
        return new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: level,
            exp: 0,
            jobLevel: 1,
            gold: 100,
            status: new Status(10, 10, 10, 10, 10, 10, 10),
            job: Job.Apprentice,
            moveSet: new MoveSet(),
            masteredJobs: masteredJobs);
    }
}
