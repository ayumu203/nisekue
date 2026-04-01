namespace server.infrastructure.ranking;

public sealed class RankingEntryEntity
{
    public Guid Id { get; set; }
    public Guid SnapshotId { get; set; }
    public string RankingType { get; set; } = string.Empty;
    public string PeriodKind { get; set; } = string.Empty;
    public string? CombatIndexRank { get; set; }
    public Guid PlayerId { get; set; }
    public int RankPosition { get; set; }
    public long Score { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
