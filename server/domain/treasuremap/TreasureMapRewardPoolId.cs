namespace server.domain.treasuremap;

public readonly record struct TreasureMapRewardPoolId(Guid Value)
{
    public static TreasureMapRewardPoolId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
