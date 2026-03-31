using System;
using System.Text.Json;
using server.domain.player;

namespace server.domain.treasuremap;

public sealed class TreasureMapClaimHistory
{
    public TreasureMapClaimHistory(
        Guid id,
        PlayerId playerId,
        TreasureMapExpeditionId expeditionId,
        JsonDocument rewardSummaryJson,
        DateTimeOffset claimedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Claim history id must not be empty.", nameof(id));
        }

        Id = id;
        PlayerId = playerId;
        ExpeditionId = expeditionId;
        RewardSummaryJson = rewardSummaryJson ?? throw new ArgumentNullException(nameof(rewardSummaryJson));
        ClaimedAt = claimedAt;
    }

    public Guid Id { get; }
    public PlayerId PlayerId { get; }
    public TreasureMapExpeditionId ExpeditionId { get; }
    public JsonDocument RewardSummaryJson { get; }
    public DateTimeOffset ClaimedAt { get; }
}
