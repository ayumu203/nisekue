using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.infrastructure;

namespace server.application.ranking;

public sealed class RankingReadService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    CombatIndexCalculator combatIndexCalculator,
    CombatIndexRankEvaluator combatIndexRankEvaluator)
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

        var rebirthCounts = await dbContext.PlayerMasterJobs
            .AsNoTracking()
            .GroupBy(x => x.PlayerId)
            .Select(g => new { g.Key, RebirthCount = Math.Max(0, g.Count() - 1) })
            .ToDictionaryAsync(x => x.Key, x => x.RebirthCount);

        var rawRows = await (
            from entry in dbContext.RankingEntries.AsNoTracking()
            join player in dbContext.Players.AsNoTracking() on entry.PlayerId equals player.Id
            where entry.SnapshotId == latestSnapshot.Id
            orderby entry.RankingType, entry.PeriodKind, entry.CombatIndexRank, entry.RankPosition
            select new
            {
                entry.RankingType,
                entry.PeriodKind,
                entry.CombatIndexRank,
                entry.RankPosition,
                entry.Score,
                PlayerId = player.Id,
                PlayerName = player.Name,
                PlayerImagePath = player.ImagePath,
                player.Level,
                player.Job,
                JobDisplayName = JobDisplayNames.GetDisplayName(player.Job),
                player.MaxHp,
                player.MaxMp,
                player.Strength,
                player.Defense,
                player.Intelligence,
                player.Luck,
                player.Speed
            })
            .ToListAsync();

        var rows = rawRows
            .Select(x =>
            {
                var status = new Status(
                    maxHp: x.MaxHp,
                    maxMp: x.MaxMp,
                    strength: x.Strength,
                    defense: x.Defense,
                    intelligence: x.Intelligence,
                    luck: x.Luck,
                    speed: x.Speed);
                var playerCombatIndexRank = combatIndexRankEvaluator.Evaluate(combatIndexCalculator.Calculate(status)).ToString();

                return new RankingRowView(
                    x.RankingType,
                    x.PeriodKind,
                    x.CombatIndexRank,
                    x.RankPosition,
                    x.Score,
                    x.PlayerId,
                    x.PlayerName,
                    x.PlayerImagePath,
                    x.Level,
                    x.Job,
                    x.JobDisplayName,
                    rebirthCounts.GetValueOrDefault(x.PlayerId, 0),
                    playerCombatIndexRank);
            })
            .ToList();

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
    string? PlayerImagePath,
    int Level,
    Job Job,
    string JobDisplayName,
    int RebirthCount,
    string PlayerCombatIndexRank);
