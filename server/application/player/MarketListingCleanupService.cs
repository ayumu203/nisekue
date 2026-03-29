using server.domain.player;

namespace server.application.player;

public sealed class MarketListingCleanupService(
    IMarketListingRepository marketListingRepository,
    IPlayerEquipmentRepository playerEquipmentRepository,
    IItemDeletionLogRepository itemDeletionLogRepository)
{
    public async Task<CleanupExpiredMarketListingsResult> DeleteExpiredAsync(DateTimeOffset now)
    {
        var expiredListings = await marketListingRepository.GetExpiredAsync(now);
        if (expiredListings.Count == 0)
        {
            return new CleanupExpiredMarketListingsResult(0, 0, 0);
        }

        var deletedListings = 0;
        var deletedEquipments = 0;
        var deletedItemQuantity = 0;

        foreach (var listing in expiredListings)
        {
            var itemIdentifier = await DeleteListedEquipmentIfNeededAsync(listing);
            if (listing.PlayerEquipmentId is not null && itemIdentifier.StartsWith("equipment:", StringComparison.Ordinal))
            {
                deletedEquipments++;
            }

            if (listing.ItemId is not null)
            {
                deletedItemQuantity += listing.RemainingQuantity;
                itemIdentifier = $"item:{listing.ItemId.Value.Value}";
            }

            await itemDeletionLogRepository.AddAsync(new ItemDeletionLog(
                Guid.NewGuid(),
                listing.SellerId,
                itemIdentifier,
                listing.RemainingQuantity,
                "expired_listing_cleanup",
                now));

            await marketListingRepository.DeleteAsync(listing.Id);
            deletedListings++;
        }

        return new CleanupExpiredMarketListingsResult(deletedListings, deletedEquipments, deletedItemQuantity);
    }

    private async Task<string> DeleteListedEquipmentIfNeededAsync(MarketListing listing)
    {
        if (listing.PlayerEquipmentId is null)
        {
            return $"item:{listing.ItemId!.Value.Value}";
        }

        var equipment = await playerEquipmentRepository.GetAsync(listing.PlayerEquipmentId.Value);
        if (equipment is null || equipment.PlayerId != listing.SellerId)
        {
            return $"equipment-listing:{listing.PlayerEquipmentId.Value.Value}";
        }

        await playerEquipmentRepository.DeleteAsync(equipment.Id);
        return $"equipment:{equipment.EquipmentId.Value}";
    }
}

public sealed record CleanupExpiredMarketListingsResult(
    int DeletedListings,
    int DeletedEquipments,
    int DeletedItemQuantity);
