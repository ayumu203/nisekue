using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using server.domain.player;
using static server.shared.constants.player.PlayerCacheConstants;

namespace server.infrastructure.player;

public class DbPlayerEquipmentRepository(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IMemoryCache cache) : IPlayerEquipmentRepository
{
    private static string EquippedKey(Guid playerId) => $"player:equipped:{playerId}";

    public async Task<IReadOnlyList<PlayerEquipment>> GetByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.PlayerEquipments
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId.Value)
            .OrderBy(x => x.EquipmentType)
            .ThenBy(x => x.AcquiredAt)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IReadOnlyList<PlayerEquipment>> GetEquippedByPlayerAsync(PlayerId playerId)
    {
        var key = EquippedKey(playerId.Value);
        if (cache.TryGetValue(key, out IReadOnlyList<PlayerEquipmentEntity>? cachedEntities))
        {
            return cachedEntities!.Select(MapToDomain).ToArray();
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.PlayerEquipments
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId.Value && x.EquipmentStatus == (int)EquipmentStatus.Equipped)
            .OrderBy(x => x.EquipmentType)
            .ThenBy(x => x.AcquiredAt)
            .ThenBy(x => x.Id)
            .ToListAsync();

        cache.Set(key, (IReadOnlyList<PlayerEquipmentEntity>)entities, CacheTtl);
        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<PlayerEquipment?> GetAsync(PlayerEquipmentId playerEquipmentId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PlayerEquipments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == playerEquipmentId.Value);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task SaveAsync(IReadOnlyList<PlayerEquipment> playerEquipments)
    {
        ArgumentNullException.ThrowIfNull(playerEquipments);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        foreach (var playerEquipment in playerEquipments)
        {
            var existing = await dbContext.PlayerEquipments
                .SingleOrDefaultAsync(x => x.Id == playerEquipment.Id.Value);

            if (existing is null)
            {
                dbContext.PlayerEquipments.Add(new PlayerEquipmentEntity
                {
                    Id = playerEquipment.Id.Value,
                    PlayerId = playerEquipment.PlayerId.Value,
                    EquipmentId = playerEquipment.EquipmentId.Value,
                    EquipmentType = (int)playerEquipment.Type,
                    EquipmentStatus = (int)playerEquipment.Status,
                    Durability = playerEquipment.Durability,
                    Mastery = playerEquipment.Mastery,
                    PlusValue = playerEquipment.PlusValue,
                    AcquiredAt = playerEquipment.AcquiredAt,
                    UpdatedAt = playerEquipment.UpdatedAt
                });

                continue;
            }

            existing.PlayerId = playerEquipment.PlayerId.Value;
            existing.EquipmentStatus = (int)playerEquipment.Status;
            existing.Durability = playerEquipment.Durability;
            existing.Mastery = playerEquipment.Mastery;
            existing.PlusValue = playerEquipment.PlusValue;
            existing.UpdatedAt = playerEquipment.UpdatedAt;
        }

        await dbContext.SaveChangesAsync();

        foreach (var playerId in playerEquipments.Select(x => x.PlayerId.Value).Distinct())
        {
            cache.Remove(EquippedKey(playerId));
        }
    }

    public async Task DeleteAsync(PlayerEquipmentId playerEquipmentId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.PlayerEquipments.SingleOrDefaultAsync(x => x.Id == playerEquipmentId.Value);
        if (existing is null)
        {
            return;
        }

        dbContext.PlayerEquipments.Remove(existing);
        await dbContext.SaveChangesAsync();

        cache.Remove(EquippedKey(existing.PlayerId));
    }

    private static PlayerEquipment MapToDomain(PlayerEquipmentEntity entity)
    {
        return new PlayerEquipment(
            new PlayerEquipmentId(entity.Id),
            new PlayerId(entity.PlayerId),
            new EquipmentId(entity.EquipmentId),
            (EquipmentType)entity.EquipmentType,
            (EquipmentStatus)entity.EquipmentStatus,
            entity.Durability,
            entity.Mastery,
            entity.PlusValue,
            entity.AcquiredAt,
            entity.UpdatedAt);
    }
}
