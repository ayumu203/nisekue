using System;
using server.domain.player;
using server.domain.treasuremap.enums;

namespace server.domain.treasuremap;

public sealed class TreasureMapExpedition
{
    public TreasureMapExpedition(
        TreasureMapExpeditionId id,
        TreasureMapId mapId,
        PlayerId playerId,
        DateTimeOffset startedAt,
        DateTimeOffset endsAt,
        TreasureMapExpeditionStatus status = TreasureMapExpeditionStatus.InProgress,
        TreasureMapRewardResult? rewardResult = null,
        bool rewardClaimed = false,
        DateTimeOffset? completedAt = null)
    {
        if (endsAt <= startedAt)
        {
            throw new ArgumentException("EndsAt must be after StartedAt.");
        }

        Id = id;
        MapId = mapId;
        PlayerId = playerId;
        StartedAt = startedAt;
        EndsAt = endsAt;
        Status = status;
        RewardResult = rewardResult;
        RewardClaimed = rewardClaimed;
        CompletedAt = completedAt;
    }

    public TreasureMapExpeditionId Id { get; }
    public TreasureMapId MapId { get; }
    public PlayerId PlayerId { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset EndsAt { get; private set; }
    public TreasureMapExpeditionStatus Status { get; private set; }
    public TreasureMapRewardResult? RewardResult { get; private set; }
    public bool RewardClaimed { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public void MarkCompleted(TreasureMapRewardResult rewardResult, DateTimeOffset completedAt)
    {
        EnsureInProgress();
        RewardResult = rewardResult ?? throw new ArgumentNullException(nameof(rewardResult));
        Status = TreasureMapExpeditionStatus.Completed;
        CompletedAt = completedAt;
    }

    public void MarkFailed(DateTimeOffset completedAt)
    {
        EnsureInProgress();
        Status = TreasureMapExpeditionStatus.Failed;
        CompletedAt = completedAt;
    }

    public void ClaimReward()
    {
        if (Status != TreasureMapExpeditionStatus.Completed)
        {
            throw new InvalidOperationException("Only completed expeditions can have their rewards claimed.");
        }

        if (RewardClaimed)
        {
            throw new InvalidOperationException("Rewards have already been claimed.");
        }

        RewardClaimed = true;
        Status = TreasureMapExpeditionStatus.Claimed;
    }

    public void AdvanceTime(TimeSpan duration)
    {
        EnsureInProgress();
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Advance duration must be positive.");
        }

        EndsAt -= duration;
    }

    private void EnsureInProgress()
    {
        if (Status != TreasureMapExpeditionStatus.InProgress)
        {
            throw new InvalidOperationException("Expedition is not in progress.");
        }
    }
}
