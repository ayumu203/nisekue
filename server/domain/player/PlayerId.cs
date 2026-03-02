namespace server.domain.player;

public readonly record struct PlayerId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
