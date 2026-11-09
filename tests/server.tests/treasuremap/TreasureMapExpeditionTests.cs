using FluentAssertions;
using server.domain.player;
using server.domain.treasuremap;
using server.domain.treasuremap.enums;
using Xunit;

namespace server.tests.treasuremap;

public class TreasureMapExpeditionTests
{
    [Fact]
    public void MarkCompleted_WhenInProgress_SetsCompletedStatusAndReward()
    {
        var expedition = CreateInProgressExpedition();
        var completedAt = DateTimeOffset.UtcNow;
        var reward = new TreasureMapRewardResult(itemIds: [3001], experiencePoints: 120, gold: 50);

        expedition.MarkCompleted(reward, completedAt);

        expedition.Status.Should().Be(TreasureMapExpeditionStatus.Completed);
        expedition.CompletedAt.Should().Be(completedAt);
        expedition.RewardResult.Should().NotBeNull();
        expedition.RewardResult!.ExperiencePoints.Should().Be(120);
        expedition.RewardResult.Gold.Should().Be(50);
    }

    [Fact]
    public void ClaimReward_WhenStatusIsCompleted_SetsClaimedStatus()
    {
        var expedition = CreateInProgressExpedition();
        expedition.MarkCompleted(new TreasureMapRewardResult(gold: 100), DateTimeOffset.UtcNow);

        expedition.ClaimReward();

        expedition.Status.Should().Be(TreasureMapExpeditionStatus.Claimed);
        expedition.RewardClaimed.Should().BeTrue();
    }

    [Fact]
    public void ClaimReward_WhenStatusIsNotCompleted_ThrowsInvalidOperationException()
    {
        var expedition = CreateInProgressExpedition();

        var act = () => expedition.ClaimReward();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only completed expeditions*");
    }

    [Fact]
    public void AdvanceTime_WhenDurationExceedsRemaining_ClampsEndsAtToStartedAt()
    {
        var expedition = CreateInProgressExpedition();

        expedition.AdvanceTime(TimeSpan.FromMinutes(5));
        expedition.AdvanceTime(TimeSpan.FromMinutes(5));
        expedition.AdvanceTime(TimeSpan.FromMinutes(5));

        expedition.EndsAt.Should().Be(expedition.StartedAt);
    }

    [Fact]
    public void Constructor_WhenEndsAtEqualsStartedAt_Rehydrates()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => new TreasureMapExpedition(
            TreasureMapExpeditionId.New(),
            new TreasureMapId(4001),
            new PlayerId(Guid.NewGuid()),
            now,
            now);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WhenEndsAtIsBeforeStartedAt_ThrowsArgumentException()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => new TreasureMapExpedition(
            TreasureMapExpeditionId.New(),
            new TreasureMapId(4001),
            new PlayerId(Guid.NewGuid()),
            now,
            now.AddSeconds(-1));

        act.Should().Throw<ArgumentException>();
    }

    private static TreasureMapExpedition CreateInProgressExpedition()
    {
        var now = DateTimeOffset.UtcNow;
        return new TreasureMapExpedition(
            TreasureMapExpeditionId.New(),
            new TreasureMapId(4001),
            new PlayerId(Guid.NewGuid()),
            now,
            now.AddMinutes(10));
    }
}
