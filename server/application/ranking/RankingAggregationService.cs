using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.domain.quest.enums;
using server.infrastructure;
using server.infrastructure.ranking;

namespace server.application.ranking;

public sealed class RankingAggregationService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    CombatIndexCalculator combatIndexCalculator,
    CombatIndexRankEvaluator combatIndexRankEvaluator)
{
    public async Task<RankingRebuildResult> RebuildAsync(DateTimeOffset now)
    {
        var snapshotAt = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);
        var dailyFrom = now.AddDays(-1);
        var weeklyFrom = now.AddDays(-7);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var tx = await dbContext.Database.BeginTransactionAsync();

        var keepSnapshotIds = await dbContext.RankingSnapshots
            .OrderByDescending(x => x.SnapshotAt)
            .ThenByDescending(x => x.Id)
            .Take(10)
            .Select(x => x.Id)
            .ToListAsync();

        if (keepSnapshotIds.Count > 0)
        {
            await dbContext.RankingEntries
                .Where(x => !keepSnapshotIds.Contains(x.SnapshotId))
                .ExecuteDeleteAsync();
            await dbContext.RankingSnapshots
                .Where(x => !keepSnapshotIds.Contains(x.Id))
                .ExecuteDeleteAsync();
        }

        var players = await dbContext.Players
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.ImagePath,
                x.RebirthCount,
                x.EndlessBestFloor,
                x.MaxHp,
                x.MaxMp,
                x.Strength,
                x.Defense,
                x.Intelligence,
                x.Luck,
                x.Speed,
                x.TrainingBattleCount
            })
            .ToListAsync();

        var questClearTotal = await BuildQuestClearCountAsync(dbContext, null);
        var questClearWeekly = await BuildQuestClearCountAsync(dbContext, weeklyFrom);
        var questClearDaily = await BuildQuestClearCountAsync(dbContext, dailyFrom);

        var treasureMapUsageTotal = await BuildTreasureMapUsageCountAsync(dbContext, null);
        var treasureMapUsageWeekly = await BuildTreasureMapUsageCountAsync(dbContext, weeklyFrom);
        var treasureMapUsageDaily = await BuildTreasureMapUsageCountAsync(dbContext, dailyFrom);
        var petBattleRatings = await BuildPetBattleRatingsAsync(dbContext);

        var playerRows = players.Select(x =>
        {
            var status = new Status(
                x.MaxHp,
                x.MaxMp,
                x.Strength,
                x.Defense,
                x.Intelligence,
                x.Luck,
                x.Speed);
            var combatIndex = combatIndexCalculator.Calculate(status);
            var combatRank = combatIndexRankEvaluator.Evaluate(combatIndex);

            return new PlayerRankingRow(
                x.Id,
                x.Name,
                x.ImagePath,
                status,
                combatIndex,
                combatRank,
                x.TrainingBattleCount,
                x.RebirthCount,
                x.EndlessBestFloor,
                petBattleRatings.GetValueOrDefault(x.Id, 0),
                questClearTotal.GetValueOrDefault(x.Id, 0),
                questClearWeekly.GetValueOrDefault(x.Id, 0),
                questClearDaily.GetValueOrDefault(x.Id, 0),
                treasureMapUsageTotal.GetValueOrDefault(x.Id, 0),
                treasureMapUsageWeekly.GetValueOrDefault(x.Id, 0),
                treasureMapUsageDaily.GetValueOrDefault(x.Id, 0));
        }).ToArray();

        var snapshotId = Guid.NewGuid();
        dbContext.RankingSnapshots.Add(new RankingSnapshotEntity
        {
            Id = snapshotId,
            SnapshotAt = snapshotAt,
            IntervalHours = 6,
            CreatedAt = now
        });

        var entries = new List<RankingEntryEntity>();
        AddTop(entries, snapshotId, now, RankingConstants.CombatIndexTop, RankingPeriodKind.Total, null, playerRows, x => x.CombatIndex, 10);
        AddTop(entries, snapshotId, now, RankingConstants.StatusMaxHpTop, RankingPeriodKind.Total, null, playerRows, x => x.Status.MaxHp, 10);
        AddTop(entries, snapshotId, now, RankingConstants.StatusMaxMpTop, RankingPeriodKind.Total, null, playerRows, x => x.Status.MaxMp, 10);
        AddTop(entries, snapshotId, now, RankingConstants.StatusStrengthTop, RankingPeriodKind.Total, null, playerRows, x => x.Status.Strength, 10);
        AddTop(entries, snapshotId, now, RankingConstants.StatusDefenseTop, RankingPeriodKind.Total, null, playerRows, x => x.Status.Defense, 10);
        AddTop(entries, snapshotId, now, RankingConstants.StatusIntelligenceTop, RankingPeriodKind.Total, null, playerRows, x => x.Status.Intelligence, 10);
        AddTop(entries, snapshotId, now, RankingConstants.StatusLuckTop, RankingPeriodKind.Total, null, playerRows, x => x.Status.Luck, 10);
        AddTop(entries, snapshotId, now, RankingConstants.StatusSpeedTop, RankingPeriodKind.Total, null, playerRows, x => x.Status.Speed, 10);
        AddTop(entries, snapshotId, now, RankingConstants.RebirthCountTop, RankingPeriodKind.Total, null, playerRows, x => x.RebirthCount, 10);
        AddTop(entries, snapshotId, now, RankingConstants.EndlessMaxFloorTop, RankingPeriodKind.Total, null, playerRows, x => x.EndlessBestFloor, 10);
        AddTop(entries, snapshotId, now, RankingConstants.PetBattleRatingTop, RankingPeriodKind.Total, null, playerRows, x => x.PetBattleRating, 10);

        AddTop(entries, snapshotId, now, RankingConstants.QuestClearByCombatRank, RankingPeriodKind.Total, null, playerRows, x => x.QuestClearTotal, 5);
        AddTop(entries, snapshotId, now, RankingConstants.QuestClearByCombatRank, RankingPeriodKind.Weekly, null, playerRows, x => x.QuestClearWeekly, 5);
        AddTop(entries, snapshotId, now, RankingConstants.QuestClearByCombatRank, RankingPeriodKind.Daily, null, playerRows, x => x.QuestClearDaily, 5);

        AddTop(entries, snapshotId, now, RankingConstants.TrainingBattleByCombatRank, RankingPeriodKind.Total, null, playerRows, x => x.TrainingBattleCount, 5);
        AddTop(entries, snapshotId, now, RankingConstants.TrainingBattleByCombatRank, RankingPeriodKind.Weekly, null, playerRows, x => x.TrainingBattleCount, 5);
        AddTop(entries, snapshotId, now, RankingConstants.TrainingBattleByCombatRank, RankingPeriodKind.Daily, null, playerRows, x => x.TrainingBattleCount, 5);

        AddTop(entries, snapshotId, now, RankingConstants.TreasureMapUsageByCombatRank, RankingPeriodKind.Total, null, playerRows, x => x.TreasureMapUsageTotal, 5);
        AddTop(entries, snapshotId, now, RankingConstants.TreasureMapUsageByCombatRank, RankingPeriodKind.Weekly, null, playerRows, x => x.TreasureMapUsageWeekly, 5);
        AddTop(entries, snapshotId, now, RankingConstants.TreasureMapUsageByCombatRank, RankingPeriodKind.Daily, null, playerRows, x => x.TreasureMapUsageDaily, 5);

        foreach (var rank in Enum.GetValues<StatusRank>())
        {
            AddTop(entries, snapshotId, now, RankingConstants.QuestClearByCombatRank, RankingPeriodKind.Total, rank, playerRows, x => x.QuestClearTotal, 5);
            AddTop(entries, snapshotId, now, RankingConstants.QuestClearByCombatRank, RankingPeriodKind.Weekly, rank, playerRows, x => x.QuestClearWeekly, 5);
            AddTop(entries, snapshotId, now, RankingConstants.QuestClearByCombatRank, RankingPeriodKind.Daily, rank, playerRows, x => x.QuestClearDaily, 5);

            // 訓練バトル回数は現在累計のみ保持しているため、週次/日次は累計値を使用。
            AddTop(entries, snapshotId, now, RankingConstants.TrainingBattleByCombatRank, RankingPeriodKind.Total, rank, playerRows, x => x.TrainingBattleCount, 5);
            AddTop(entries, snapshotId, now, RankingConstants.TrainingBattleByCombatRank, RankingPeriodKind.Weekly, rank, playerRows, x => x.TrainingBattleCount, 5);
            AddTop(entries, snapshotId, now, RankingConstants.TrainingBattleByCombatRank, RankingPeriodKind.Daily, rank, playerRows, x => x.TrainingBattleCount, 5);

            AddTop(entries, snapshotId, now, RankingConstants.TreasureMapUsageByCombatRank, RankingPeriodKind.Total, rank, playerRows, x => x.TreasureMapUsageTotal, 5);
            AddTop(entries, snapshotId, now, RankingConstants.TreasureMapUsageByCombatRank, RankingPeriodKind.Weekly, rank, playerRows, x => x.TreasureMapUsageWeekly, 5);
            AddTop(entries, snapshotId, now, RankingConstants.TreasureMapUsageByCombatRank, RankingPeriodKind.Daily, rank, playerRows, x => x.TreasureMapUsageDaily, 5);
        }

        dbContext.RankingEntries.AddRange(entries);
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        return new RankingRebuildResult(snapshotAt, entries.Count);
    }

    private static void AddTop(
        ICollection<RankingEntryEntity> entries,
        Guid snapshotId,
        DateTimeOffset now,
        string rankingType,
        RankingPeriodKind periodKind,
        StatusRank? combatRank,
        IReadOnlyCollection<PlayerRankingRow> players,
        Func<PlayerRankingRow, int> selector,
        int topCount)
    {
        var ranked = players
            .Where(x => combatRank is null || x.CombatIndexRank == combatRank.Value)
            .Select(x => new { x.PlayerId, Score = (long)selector(x) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.PlayerId)
            .Take(topCount)
            .ToArray();

        for (var i = 0; i < ranked.Length; i++)
        {
            entries.Add(new RankingEntryEntity
            {
                Id = Guid.NewGuid(),
                SnapshotId = snapshotId,
                RankingType = rankingType,
                PeriodKind = periodKind.ToString(),
                CombatIndexRank = combatRank?.ToString(),
                PlayerId = ranked[i].PlayerId,
                RankPosition = i + 1,
                Score = ranked[i].Score,
                CreatedAt = now
            });
        }
    }

    private static async Task<Dictionary<Guid, int>> BuildQuestClearCountAsync(AppDbContext dbContext, DateTimeOffset? from)
    {
        var query =
            from run in dbContext.QuestRuns.AsNoTracking()
            join snapshot in dbContext.QuestRunPartySnapshots.AsNoTracking() on run.Id equals snapshot.RunId
            where run.Status == (int)QuestRunStatus.Succeeded
                && snapshot.ParticipantType == (int)ParticipantType.Player
            select new
            {
                PlayerId = snapshot.ParticipantId,
                EndedAt = run.EndedAt ?? run.StartedAt
            };

        if (from is not null)
        {
            var fromValue = from.Value;
            query = query.Where(x => x.EndedAt >= fromValue);
        }

        return await query
            .GroupBy(x => x.PlayerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    private static async Task<Dictionary<Guid, int>> BuildTreasureMapUsageCountAsync(AppDbContext dbContext, DateTimeOffset? from)
    {
        var query = dbContext.TreasureMapExpeditions
            .AsNoTracking()
            .Select(x => new { x.PlayerId, x.StartedAt });

        if (from is not null)
        {
            var fromValue = from.Value;
            query = query.Where(x => x.StartedAt >= fromValue);
        }

        return await query
            .GroupBy(x => x.PlayerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    private static async Task<Dictionary<Guid, int>> BuildPetBattleRatingsAsync(AppDbContext dbContext)
    {
        return await dbContext.PlayerPetBattleStats
            .AsNoTracking()
            .ToDictionaryAsync(x => x.PlayerId, x => x.Rating);
    }

    private sealed record PlayerRankingRow(
        Guid PlayerId,
        string Name,
        string? ImagePath,
        Status Status,
        int CombatIndex,
        StatusRank CombatIndexRank,
        int TrainingBattleCount,
        int RebirthCount,
        int EndlessBestFloor,
        int PetBattleRating,
        int QuestClearTotal,
        int QuestClearWeekly,
        int QuestClearDaily,
        int TreasureMapUsageTotal,
        int TreasureMapUsageWeekly,
        int TreasureMapUsageDaily);
}
