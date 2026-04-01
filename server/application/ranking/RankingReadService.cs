using Microsoft.EntityFrameworkCore;
using server.infrastructure;

namespace server.application.ranking;

public sealed class RankingReadService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<(DateTimeOffset? SnapshotAt, IReadOnlyList<RankingRowView> Rows)> GetLatestAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var latestSnapshot = await dbContext.RankingSnapshots
            .AsNoTracking()
            .OrderByDescending(x => x.SnapshotAt)
            .FirstOrDefaultAsync();

        if (latestSnapshot is null)
        {
            return (null, []);
        }

        var rows = await (
            from entry in dbContext.RankingEntries.AsNoTracking()
            join player in dbContext.Players.AsNoTracking() on entry.PlayerId equals player.Id
            where entry.SnapshotId == latestSnapshot.Id
            orderby entry.RankingType, entry.PeriodKind, entry.CombatIndexRank, entry.RankPosition
            select new RankingRowView(
                entry.RankingType,
                entry.PeriodKind,
                entry.CombatIndexRank,
                entry.RankPosition,
                entry.Score,
                player.Id,
                player.Name,
                player.ImagePath))
            .ToListAsync();

        return (latestSnapshot.SnapshotAt, rows);
    }
}

public sealed record RankingRowView(
    string RankingType,
    string PeriodKind,
    string? CombatIndexRank,
    int RankPosition,
    long Score,
    Guid PlayerId,
    string PlayerName,
    string? PlayerImagePath);
