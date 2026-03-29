using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;

namespace server.application.player;

public sealed class MarketListingCleanupService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<CleanupExpiredMarketListingsResult> DeleteExpiredAsync(DateTimeOffset now)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;

        var expiredListings = await dbContext.MarketListings
            .Where(x => x.ExpiresAt <= now && x.RemainingQuantity > 0)
            .OrderBy(x => x.ExpiresAt)
            .ThenBy(x => x.ListedAt)
            .ToListAsync();
        if (expiredListings.Count == 0)
        {
            return new CleanupExpiredMarketListingsResult(0, 0, 0);
        }

        var deletedListings = 0;
        var deletedEquipments = 0;
        var deletedItemQuantity = 0;

        foreach (var listing in expiredListings)
        {
            var itemIdentifier = await DeleteListedEquipmentIfNeededAsync(dbContext, listing);
            if (listing.PlayerEquipmentId is not null && itemIdentifier.StartsWith("equipment:", StringComparison.Ordinal))
            {
                deletedEquipments++;
            }

            if (listing.ItemId is not null)
            {
                deletedItemQuantity += listing.RemainingQuantity;
                itemIdentifier = $"item:{listing.ItemId.Value}";
            }

            dbContext.ItemDeletionLogs.Add(new ItemDeletionLogEntity
            {
                Id = Guid.NewGuid(),
                PlayerId = listing.SellerId,
                ItemIdentifier = itemIdentifier,
                Quantity = listing.RemainingQuantity,
                Reason = "expired_listing_cleanup",
                DeletedAt = now
            });

            dbContext.MarketListings.Remove(listing);
            deletedListings++;
        }

        await dbContext.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return new CleanupExpiredMarketListingsResult(deletedListings, deletedEquipments, deletedItemQuantity);
    }

    private static async Task<string> DeleteListedEquipmentIfNeededAsync(AppDbContext dbContext, MarketListingEntity listing)
    {
        if (listing.PlayerEquipmentId is null)
        {
            return $"item:{listing.ItemId!.Value}";
        }

        var equipment = await dbContext.PlayerEquipments.SingleOrDefaultAsync(x => x.Id == listing.PlayerEquipmentId.Value);
        if (equipment is null || equipment.PlayerId != listing.SellerId)
        {
            return $"equipment-listing:{listing.PlayerEquipmentId.Value}";
        }

        dbContext.PlayerEquipments.Remove(equipment);
        return $"equipment:{equipment.EquipmentId}";
    }
}

public sealed record CleanupExpiredMarketListingsResult(
    int DeletedListings,
    int DeletedEquipments,
    int DeletedItemQuantity);
