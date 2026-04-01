namespace server.application.ranking;

public sealed record RankingRebuildResult(
    DateTimeOffset SnapshotAt,
    int CreatedEntries);
