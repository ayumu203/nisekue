using server.domain.pet;
using server.domain.player;
using server.domain.quest;

namespace server.application.pet;

public record PlayerPetView(
    Guid PetId,
    int EnemyDefinitionId,
    string Name,
    string? ImagePath,
    int Level,
    PetBonusStatus BonusStatus,
    Status TotalStatus,
    bool IsActive,
    DateTimeOffset CapturedAt);

public class PetService(
    IPlayerPetRepository playerPetRepository,
    IPlayerRepository playerRepository,
    IQuestEnemyDefinitionRepository questEnemyDefinitionRepository)
{
    public async Task<IReadOnlyList<PlayerPetView>> GetPetsAsync(PlayerId playerId)
    {
        var pets = await playerPetRepository.GetByPlayerAsync(playerId);
        var views = new List<PlayerPetView>(pets.Count);
        foreach (var pet in pets)
        {
            views.Add(await MapToViewAsync(pet));
        }

        return views;
    }

    public async Task<PlayerPetView> TrainAsync(PlayerId playerId, PlayerPetId petId)
    {
        var pet = await GetOwnedPetAsync(playerId, petId);
        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

        player.SpendGold(PetConstants.TrainingCostGold);
        pet.Train(player.Status, DateTimeOffset.UtcNow);

        await playerRepository.SaveAsync(player);
        await playerPetRepository.SaveAsync([pet]);
        return await MapToViewAsync(pet);
    }

    public async Task ReleaseAsync(PlayerId playerId, PlayerPetId petId)
    {
        var pet = await GetOwnedPetAsync(playerId, petId);
        await playerPetRepository.DeleteAsync(pet.Id);
    }

    public async Task<IReadOnlyList<PlayerPetView>> ActivateAsync(PlayerId playerId, PlayerPetId petId)
    {
        var pets = await playerPetRepository.GetByPlayerAsync(playerId);
        if (pets.All(x => x.Id != petId))
        {
            throw new KeyNotFoundException("ペットが見つかりません。");
        }

        var now = DateTimeOffset.UtcNow;
        var changed = new List<PlayerPet>();
        foreach (var pet in pets)
        {
            if (pet.Id == petId && !pet.IsActive)
            {
                pet.Activate(now);
                changed.Add(pet);
            }
            else if (pet.Id != petId && pet.IsActive)
            {
                pet.Deactivate(now);
                changed.Add(pet);
            }
        }

        if (changed.Count > 0)
        {
            await playerPetRepository.SaveAsync(changed);
        }

        var views = new List<PlayerPetView>(pets.Count);
        foreach (var pet in pets)
        {
            views.Add(await MapToViewAsync(pet));
        }

        return views;
    }

    public async Task<IReadOnlyList<PlayerPetView>> DeactivateAsync(PlayerId playerId, PlayerPetId petId)
    {
        var pet = await GetOwnedPetAsync(playerId, petId);
        if (pet.IsActive)
        {
            pet.Deactivate(DateTimeOffset.UtcNow);
            await playerPetRepository.SaveAsync([pet]);
        }

        return await GetPetsAsync(playerId);
    }

    private async Task<PlayerPet> GetOwnedPetAsync(PlayerId playerId, PlayerPetId petId)
    {
        var pet = await playerPetRepository.GetAsync(petId);
        if (pet is null || pet.PlayerId != playerId)
        {
            throw new KeyNotFoundException("ペットが見つかりません。");
        }

        return pet;
    }

    private async Task<PlayerPetView> MapToViewAsync(PlayerPet pet)
    {
        var definition = await questEnemyDefinitionRepository.GetAsync(pet.EnemyDefinitionId);
        var totalStatus = definition is null
            ? new Status(
                maxHp: Math.Max(1, pet.BonusStatus.MaxHp),
                maxMp: pet.BonusStatus.MaxMp,
                strength: pet.BonusStatus.Strength,
                defense: pet.BonusStatus.Defense,
                intelligence: pet.BonusStatus.Intelligence,
                luck: pet.BonusStatus.Luck,
                speed: pet.BonusStatus.Speed)
            : PetStatusResolver.Resolve(pet, definition);

        return new PlayerPetView(
            pet.Id.Value,
            pet.EnemyDefinitionId.Value,
            definition?.Name ?? "不明なモンスター",
            definition?.ImagePath,
            definition?.Level ?? 1,
            pet.BonusStatus,
            totalStatus,
            pet.IsActive,
            pet.CapturedAt);
    }
}
