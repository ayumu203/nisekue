using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.domain.treasuremap;

namespace server.infrastructure.treasuremap;

public sealed class DbTreasureMapClaimHistoryRepository(IDbContextFactory<AppDbContext> dbContextFactory) : ITreasureMapClaimHistoryRepository
{
    public async Task<bool> ExistsByExpeditionAsync(TreasureMapExpeditionId expeditionId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.TreasureMapClaimHistories
            .AsNoTracking()
            .AnyAsync(x => x.ExpeditionId == expeditionId.Value);
    }

    public async Task<IReadOnlyList<TreasureMapClaimHistory>> ListByPlayerAsync(PlayerId playerId, int take = 50)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.TreasureMapClaimHistories
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId.Value)
            .OrderByDescending(x => x.ClaimedAt)
            .Take(Math.Max(1, take))
            .ToListAsync();

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task AddAsync(TreasureMapClaimHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.TreasureMapClaimHistories.Add(new TreasureMapClaimHistoryEntity
        {
            Id = history.Id,
            PlayerId = history.PlayerId.Value,
            ExpeditionId = history.ExpeditionId.Value,
            RewardSummaryJson = history.RewardSummaryJson.RootElement.GetRawText(),
            ClaimedAt = history.ClaimedAt
        });

        await dbContext.SaveChangesAsync();
    }

    private static TreasureMapClaimHistory MapToDomain(TreasureMapClaimHistoryEntity entity)
    {
        return new TreasureMapClaimHistory(
            entity.Id,
            new PlayerId(entity.PlayerId),
            new TreasureMapExpeditionId(entity.ExpeditionId),
            JsonDocument.Parse(entity.RewardSummaryJson),
            entity.ClaimedAt);
    }
}
