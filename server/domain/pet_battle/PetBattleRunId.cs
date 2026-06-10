namespace server.domain.pet_battle;

public record PetBattleRunId(Guid Value)
{
    public static PetBattleRunId NewId() => new(Guid.NewGuid());
}
