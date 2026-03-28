namespace server.domain.player;

public class MarketTradeHistory(
    Guid id,
    PlayerId sellerId,
    PlayerId buyerId,
    string itemIdentifier,
    int quantity,
    int unitPrice,
    DateTimeOffset purchasedAt)
{
    public Guid Id { get; } = id;
    public PlayerId SellerId { get; } = sellerId;
    public PlayerId BuyerId { get; } = buyerId;
    public string ItemIdentifier { get; } = string.IsNullOrWhiteSpace(itemIdentifier) ? throw new ArgumentException("itemIdentifier は必須です。", nameof(itemIdentifier)) : itemIdentifier.Trim();
    public int Quantity { get; } = quantity > 0 ? quantity : throw new ArgumentOutOfRangeException(nameof(quantity), "1以上である必要があります。");
    public int UnitPrice { get; } = unitPrice > 0 ? unitPrice : throw new ArgumentOutOfRangeException(nameof(unitPrice), "1以上である必要があります。");
    public DateTimeOffset PurchasedAt { get; } = purchasedAt;
}
