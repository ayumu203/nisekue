using Microsoft.EntityFrameworkCore;
using server.domain.player;

namespace server.infrastructure.player;

public class DbMarketListingRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IMarketListingRepository
{
    public async Task<MarketListing?> GetAsync(MarketListingId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.MarketListings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyList<MarketListing>> GetActiveAsync(DateTimeOffset now)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.MarketListings
            .AsNoTracking()
            .Where(x => x.ExpiresAt > now && x.RemainingQuantity > 0)
            .OrderBy(x => x.ExpiresAt)
            .ThenBy(x => x.ListedAt)
            .ToListAsync();

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IReadOnlyList<MarketListing>> GetBySellerAsync(PlayerId sellerId, DateTimeOffset now)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.MarketListings
            .AsNoTracking()
            .Where(x => x.SellerId == sellerId.Value && x.ExpiresAt > now && x.RemainingQuantity > 0)
            .OrderByDescending(x => x.ListedAt)
            .ToListAsync();

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IReadOnlyList<MarketListing>> GetExpiredAsync(DateTimeOffset now)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.MarketListings
            .AsNoTracking()
            .Where(x => x.ExpiresAt <= now && x.RemainingQuantity > 0)
            .OrderBy(x => x.ExpiresAt)
            .ThenBy(x => x.ListedAt)
            .ToListAsync();

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task SaveAsync(MarketListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.MarketListings.SingleOrDefaultAsync(x => x.Id == listing.Id.Value);
        if (existing is null)
        {
            dbContext.MarketListings.Add(new MarketListingEntity
            {
                Id = listing.Id.Value,
                SellerId = listing.SellerId.Value,
                PlayerEquipmentId = listing.PlayerEquipmentId?.Value,
                ItemId = listing.ItemId?.Value,
                ItemName = listing.ItemName,
                FlavorText = listing.FlavorText,
                Quantity = listing.Quantity,
                RemainingQuantity = listing.RemainingQuantity,
                UnitPrice = listing.UnitPrice,
                ListedAt = listing.ListedAt,
                ExpiresAt = listing.ExpiresAt
            });
        }
        else
        {
            existing.RemainingQuantity = listing.RemainingQuantity;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(MarketListingId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.MarketListings.SingleOrDefaultAsync(x => x.Id == id.Value);
        if (existing is null)
        {
            return;
        }

        dbContext.MarketListings.Remove(existing);
        await dbContext.SaveChangesAsync();
    }

    private static MarketListing MapToDomain(MarketListingEntity entity)
    {
        return new MarketListing(
            new MarketListingId(entity.Id),
            new PlayerId(entity.SellerId),
            entity.PlayerEquipmentId is null ? null : new PlayerEquipmentId(entity.PlayerEquipmentId.Value),
            entity.ItemId is null ? null : new ItemId(entity.ItemId.Value),
            entity.ItemName,
            entity.FlavorText,
            entity.Quantity,
            entity.RemainingQuantity,
            entity.UnitPrice,
            entity.ListedAt,
            entity.ExpiresAt);
    }
}
