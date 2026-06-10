namespace server.domain.pet;

public readonly record struct PlayerPetId(Guid Value)
{
    public static PlayerPetId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
