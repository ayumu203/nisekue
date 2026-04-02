namespace server.domain.treasuremap;

public readonly record struct TreasureMapId(int Value)
{
    public override string ToString() => Value.ToString();
}
