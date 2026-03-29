using Microsoft.EntityFrameworkCore;
using server.domain.player;

namespace server.infrastructure.player;

public class DbPlayerItemStackRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IPlayerItemStackRepository
{
    public async Task<IReadOnlyList<PlayerItemStack>> GetByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.PlayerItemStacks
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId.Value)
            .OrderBy(x => x.ItemId)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<PlayerItemStack?> GetAsync(PlayerItemStackId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PlayerItemStacks
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task SaveAsync(IReadOnlyList<PlayerItemStack> playerItemStacks)
    {
        ArgumentNullException.ThrowIfNull(playerItemStacks);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        foreach (var stack in playerItemStacks)
        {
            var existing = await dbContext.PlayerItemStacks.SingleOrDefaultAsync(x => x.Id == stack.Id.Value);
            if (existing is null)
            {
                dbContext.PlayerItemStacks.Add(new PlayerItemStackEntity
                {
                    Id = stack.Id.Value,
                    PlayerId = stack.PlayerId.Value,
                    ItemId = stack.ItemId.Value,
                    Quantity = stack.Quantity,
                    UpdatedAt = stack.UpdatedAt
                });
                continue;
            }

            existing.Quantity = stack.Quantity;
            existing.UpdatedAt = stack.UpdatedAt;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(PlayerItemStackId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.PlayerItemStacks.SingleOrDefaultAsync(x => x.Id == id.Value);
        if (existing is null)
        {
            return;
        }

        dbContext.PlayerItemStacks.Remove(existing);
        await dbContext.SaveChangesAsync();
    }

    private static PlayerItemStack MapToDomain(PlayerItemStackEntity entity)
    {
        return new PlayerItemStack(
            new PlayerItemStackId(entity.Id),
            new PlayerId(entity.PlayerId),
            new ItemId(entity.ItemId),
            entity.Quantity,
            entity.UpdatedAt);
    }
}
