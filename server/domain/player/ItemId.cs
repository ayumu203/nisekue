namespace server.domain.player;

public readonly record struct ItemId(int Value)
{
    public override string ToString() => Value.ToString();
}
