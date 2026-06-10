using server.domain.pet;
using server.domain.player;

namespace server.tests.quest;

internal sealed class FakePlayerPetRepository : IPlayerPetRepository
{
    private readonly List<PlayerPet> pets;

    public FakePlayerPetRepository(params PlayerPet[] initialPets)
    {
        pets = initialPets.ToList();
    }

    public IReadOnlyList<PlayerPet> StoredPets => pets;

    public Task<IReadOnlyList<PlayerPet>> GetByPlayerAsync(PlayerId playerId)
        => Task.FromResult<IReadOnlyList<PlayerPet>>(pets.Where(x => x.PlayerId == playerId).ToArray());

    public Task<PlayerPet?> GetAsync(PlayerPetId id)
        => Task.FromResult(pets.FirstOrDefault(x => x.Id == id));

    public Task<PlayerPet?> GetStandbyByPlayerAsync(PlayerId playerId)
        => Task.FromResult(pets.FirstOrDefault(x => x.PlayerId == playerId && x.IsStandby));

    public Task<int> CountByPlayerAsync(PlayerId playerId)
        => Task.FromResult(pets.Count(x => x.PlayerId == playerId));

    public Task AddAsync(PlayerPet pet)
    {
        pets.Add(pet);
        return Task.CompletedTask;
    }

    public Task<bool> SetStandbyAsync(PlayerId playerId, PlayerPetId? standbyPetId, DateTimeOffset updatedAt)
    {
        if (standbyPetId is not null && pets.All(x => x.PlayerId != playerId || x.Id != standbyPetId.Value))
        {
            return Task.FromResult(false);
        }

        foreach (var pet in pets.Where(x => x.PlayerId == playerId))
        {
            if (standbyPetId is not null && pet.Id == standbyPetId.Value)
            {
                pet.Standby(updatedAt);
            }
            else if (pet.IsStandby)
            {
                pet.ClearStandby(updatedAt);
            }
        }

        return Task.FromResult(true);
    }

    public Task SaveAsync(IEnumerable<PlayerPet> updatedPets)
    {
        foreach (var pet in updatedPets)
        {
            var index = pets.FindIndex(x => x.Id == pet.Id);
            if (index >= 0)
            {
                pets[index] = pet;
            }
            else
            {
                pets.Add(pet);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(PlayerPetId id)
    {
        pets.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }
}
