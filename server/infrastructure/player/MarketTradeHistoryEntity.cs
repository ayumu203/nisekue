namespace server.infrastructure.player;

public class MarketTradeHistoryEntity
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    public string ItemIdentifier { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }
}
