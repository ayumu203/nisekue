namespace server.infrastructure.treasuremap;

public sealed class TreasureMapExpeditionEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public int MapId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int Status { get; set; }
    public string? RewardSummaryJson { get; set; }
    public bool RewardClaimed { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
