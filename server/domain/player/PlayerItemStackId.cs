namespace server.domain.player;

public readonly record struct PlayerItemStackId(Guid Value)
{
    public static PlayerItemStackId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
