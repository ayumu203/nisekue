namespace server.domain.treasuremap;

public readonly record struct TreasureMapId(Guid Value)
{
    public static TreasureMapId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
