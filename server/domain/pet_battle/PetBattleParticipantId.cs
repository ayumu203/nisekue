namespace server.domain.pet_battle;

public record PetBattleParticipantId(Guid Value)
{
    public static PetBattleParticipantId NewId() => new(Guid.NewGuid());
}
