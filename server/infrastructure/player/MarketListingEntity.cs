namespace server.infrastructure.player;

public class MarketListingEntity
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public Guid? PlayerEquipmentId { get; set; }
    public int? ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string FlavorText { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int RemainingQuantity { get; set; }
    public int UnitPrice { get; set; }
    public DateTimeOffset ListedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
