namespace server.domain.player;

public readonly record struct PlayerEquipmentId(Guid Value)
{
    public static PlayerEquipmentId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
