using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.domain.treasuremap;
using server.domain.treasuremap.enums;

namespace server.infrastructure.treasuremap;

public sealed class DbTreasureMapExpeditionRepository(IDbContextFactory<AppDbContext> dbContextFactory) : ITreasureMapExpeditionRepository
{
    public async Task<TreasureMapExpedition?> GetAsync(TreasureMapExpeditionId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.TreasureMapExpeditions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<TreasureMapExpedition?> GetCurrentByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.TreasureMapExpeditions
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId.Value)
            .Where(x => x.Status != (int)TreasureMapExpeditionStatus.Claimed)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync();

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyList<TreasureMapExpedition>> ListByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.TreasureMapExpeditions
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId.Value)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync();

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task SaveAsync(TreasureMapExpedition expedition)
    {
        ArgumentNullException.ThrowIfNull(expedition);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.TreasureMapExpeditions
            .SingleOrDefaultAsync(x => x.Id == expedition.Id.Value);

        var rewardSummaryJson = SerializeReward(expedition.RewardResult);
        if (existing is null)
        {
            dbContext.TreasureMapExpeditions.Add(new TreasureMapExpeditionEntity
            {
                Id = expedition.Id.Value,
                PlayerId = expedition.PlayerId.Value,
                MapId = expedition.MapId.Value,
                StartedAt = expedition.StartedAt,
                EndsAt = expedition.EndsAt,
                Status = (int)expedition.Status,
                RewardSummaryJson = rewardSummaryJson,
                RewardClaimed = expedition.RewardClaimed,
                CompletedAt = expedition.CompletedAt,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            existing.Status = (int)expedition.Status;
            existing.RewardSummaryJson = rewardSummaryJson;
            existing.RewardClaimed = expedition.RewardClaimed;
            existing.CompletedAt = expedition.CompletedAt;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }

    private static TreasureMapExpedition MapToDomain(TreasureMapExpeditionEntity entity)
    {
        return new TreasureMapExpedition(
            new TreasureMapExpeditionId(entity.Id),
            new TreasureMapId(entity.MapId),
            new PlayerId(entity.PlayerId),
            entity.StartedAt,
            entity.EndsAt,
            (TreasureMapExpeditionStatus)entity.Status,
            DeserializeReward(entity.RewardSummaryJson),
            entity.RewardClaimed,
            entity.CompletedAt);
    }

    private static string? SerializeReward(TreasureMapRewardResult? reward)
    {
        if (reward is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new
        {
            itemIds = reward.ItemIds,
            equipmentIds = reward.EquipmentIds,
            experiencePoints = reward.ExperiencePoints,
            gold = reward.Gold
        });
    }

    private static TreasureMapRewardResult? DeserializeReward(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var itemIds = root.TryGetProperty("itemIds", out var itemIdsElement)
            ? itemIdsElement.EnumerateArray().Select(x => x.GetInt32()).ToArray()
            : Array.Empty<int>();
        var equipmentIds = root.TryGetProperty("equipmentIds", out var equipmentIdsElement)
            ? equipmentIdsElement.EnumerateArray().Select(x => x.GetInt32()).ToArray()
            : Array.Empty<int>();
        var exp = root.TryGetProperty("experiencePoints", out var expElement) ? expElement.GetInt32() : 0;
        var gold = root.TryGetProperty("gold", out var goldElement) ? goldElement.GetInt32() : 0;

        return new TreasureMapRewardResult(itemIds, equipmentIds, exp, gold);
    }
}
