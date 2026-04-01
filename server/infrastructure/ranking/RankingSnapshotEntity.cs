namespace server.infrastructure.ranking;

public sealed class RankingSnapshotEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset SnapshotAt { get; set; }
    public int IntervalHours { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
