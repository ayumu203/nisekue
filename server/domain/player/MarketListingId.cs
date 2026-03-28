namespace server.domain.player;

public readonly record struct MarketListingId(Guid Value)
{
    public static MarketListingId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
