using server.domain.player;

namespace server.domain.pet;

public interface IPlayerPetRepository
{
    Task<IReadOnlyList<PlayerPet>> GetByPlayerAsync(PlayerId playerId);
    Task<PlayerPet?> GetAsync(PlayerPetId id);
    Task<PlayerPet?> GetStandbyByPlayerAsync(PlayerId playerId);
    Task<int> CountByPlayerAsync(PlayerId playerId);
    Task AddAsync(PlayerPet pet);
    Task SaveAsync(IEnumerable<PlayerPet> pets);
    Task DeleteAsync(PlayerPetId id);
}
