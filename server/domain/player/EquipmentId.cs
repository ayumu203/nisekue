namespace server.domain.player;

public readonly record struct EquipmentId(int Value)
{
    public override string ToString() => Value.ToString();
}
