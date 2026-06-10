namespace server.domain.pet_battle;

public record PetBattleRoomId(Guid Value)
{
    public static PetBattleRoomId NewId() => new(Guid.NewGuid());
}
