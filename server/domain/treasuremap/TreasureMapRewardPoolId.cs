namespace server.domain.treasuremap;

public readonly record struct TreasureMapRewardPoolId(int Value)
{
    public override string ToString() => Value.ToString();
}
