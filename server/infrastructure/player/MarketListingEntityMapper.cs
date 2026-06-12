using server.domain.player;

namespace server.infrastructure.player;

internal static class MarketListingEntityMapper
{
    public static MarketListing MapToDomain(MarketListingEntity entity)
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
