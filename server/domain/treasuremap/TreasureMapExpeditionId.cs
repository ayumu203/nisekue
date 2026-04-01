namespace server.domain.treasuremap;

public readonly record struct TreasureMapExpeditionId(Guid Value)
{
    public static TreasureMapExpeditionId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
