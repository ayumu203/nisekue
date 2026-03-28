using Microsoft.EntityFrameworkCore;
using server.domain.player;

namespace server.infrastructure.player;

public class DbPlayerEquipmentRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IPlayerEquipmentRepository
{
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
                    AcquiredAt = playerEquipment.AcquiredAt,
                    UpdatedAt = playerEquipment.UpdatedAt
                });

                continue;
            }

            existing.PlayerId = playerEquipment.PlayerId.Value;
            existing.EquipmentStatus = (int)playerEquipment.Status;
            existing.Durability = playerEquipment.Durability;
            existing.Mastery = playerEquipment.Mastery;
            existing.UpdatedAt = playerEquipment.UpdatedAt;
        }

        await dbContext.SaveChangesAsync();
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
            entity.AcquiredAt,
            entity.UpdatedAt);
    }
}
