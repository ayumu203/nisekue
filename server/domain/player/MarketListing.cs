namespace server.domain.player;

public class MarketListing(
    MarketListingId id,
    PlayerId sellerId,
    PlayerEquipmentId? playerEquipmentId,
    ItemId? itemId,
    string itemName,
    string flavorText,
    int quantity,
    int remainingQuantity,
    int unitPrice,
    DateTimeOffset listedAt,
    DateTimeOffset expiresAt)
{
    public MarketListingId Id { get; } = id;
    public PlayerId SellerId { get; } = sellerId;
    public PlayerEquipmentId? PlayerEquipmentId { get; } = playerEquipmentId;
    public ItemId? ItemId { get; } = itemId;
    public string ItemName { get; } = string.IsNullOrWhiteSpace(itemName) ? throw new ArgumentException("出品名は必須です。", nameof(itemName)) : itemName.Trim();
    public string FlavorText { get; } = string.IsNullOrWhiteSpace(flavorText) ? string.Empty : flavorText.Trim();
    public int Quantity { get; } = ValidatePositive(quantity, nameof(quantity));
    public int RemainingQuantity { get; private set; } = ValidatePositive(remainingQuantity, nameof(remainingQuantity));
    public int UnitPrice { get; } = ValidatePositive(unitPrice, nameof(unitPrice));
    public DateTimeOffset ListedAt { get; } = listedAt;
    public DateTimeOffset ExpiresAt { get; } = expiresAt;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
    public bool IsSoldOut => RemainingQuantity <= 0;

    public void Purchase(int quantity)
    {
        var validated = ValidatePositive(quantity, nameof(quantity));
        if (validated > RemainingQuantity)
        {
            throw new InvalidOperationException("残数を超えて購入できません。");
        }

        RemainingQuantity -= validated;
    }

    private static int ValidatePositive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "1以上である必要があります。");
        }

        return value;
    }
}
