using server.domain.player;

namespace server.domain.pet_battle;

public interface IPetBattleRoomRepository
{
    Task<PetBattleRoom?> GetAsync(PetBattleRoomId id);
    Task<PetBattleRoom?> GetActiveByOwnerAsync(PlayerId ownerId);
    Task SaveAsync(PetBattleRoom room);
}
