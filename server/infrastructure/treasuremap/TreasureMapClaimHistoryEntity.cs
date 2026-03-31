namespace server.infrastructure.treasuremap;

public sealed class TreasureMapClaimHistoryEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid ExpeditionId { get; set; }
    public string RewardSummaryJson { get; set; } = "{}";
    public DateTimeOffset ClaimedAt { get; set; }
}
