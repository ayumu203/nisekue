using FluentAssertions;
using server.application.pet;
using server.domain.pet;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using server.tests.quest;
using Xunit;

namespace server.tests.pet;

public class PetServiceTests
{
    private static readonly QuestEnemyDefinitionId SlimeId = new(1);

    [Fact]
    public async Task GetPetsAsync_MapsDefinitionNameAndTotalStatus()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var pet = new PlayerPet(
            PlayerPetId.New(),
            playerId,
            SlimeId,
            new PetBonusStatus(maxHp: 10, strength: 5),
            isStandby: true,
            capturedAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var service = CreateService(pet);

        var views = await service.GetPetsAsync(playerId);

        views.Should().HaveCount(1);
        views[0].Name.Should().Be("Slime");
        views[0].Level.Should().Be(5);
        views[0].IsStandby.Should().BeTrue();
        views[0].TotalStatus.MaxHp.Should().Be(110);
        views[0].TotalStatus.Strength.Should().Be(25);
        views[0].BonusStatus.MaxHp.Should().Be(10);
    }

    [Fact]
    public async Task GetPetsAsync_WithOtherPlayersPet_ReturnsEmpty()
    {
        var pet = PlayerPet.Capture(new PlayerId(Guid.NewGuid()), SlimeId, DateTimeOffset.UtcNow);
        var service = CreateService(pet);

        var views = await service.GetPetsAsync(new PlayerId(Guid.NewGuid()));

        views.Should().BeEmpty();
    }

    [Fact]
    public async Task StandbyAsync_SetsTargetStandbyAndClearsOthers()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var first = PlayerPet.Capture(playerId, SlimeId, DateTimeOffset.UtcNow);
        first.Standby(DateTimeOffset.UtcNow);
        var second = PlayerPet.Capture(playerId, SlimeId, DateTimeOffset.UtcNow);
        var service = CreateService(first, second);

        var views = await service.StandbyAsync(playerId, second.Id);

        views.Single(x => x.PetId == second.Id.Value).IsStandby.Should().BeTrue();
        views.Single(x => x.PetId == first.Id.Value).IsStandby.Should().BeFalse();
    }

    [Fact]
    public async Task StandbyAsync_WithUnknownPetId_ThrowsKeyNotFoundException()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var service = CreateService(PlayerPet.Capture(playerId, SlimeId, DateTimeOffset.UtcNow));

        var act = () => service.StandbyAsync(playerId, PlayerPetId.New());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task StandbyAsync_WithOtherPlayersPet_ThrowsKeyNotFoundException()
    {
        var otherPlayersPet = PlayerPet.Capture(new PlayerId(Guid.NewGuid()), SlimeId, DateTimeOffset.UtcNow);
        var service = CreateService(otherPlayersPet);

        var act = () => service.StandbyAsync(new PlayerId(Guid.NewGuid()), otherPlayersPet.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ClearStandbyAsync_ClearsStandbyFlag()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var pet = PlayerPet.Capture(playerId, SlimeId, DateTimeOffset.UtcNow);
        pet.Standby(DateTimeOffset.UtcNow);
        var service = CreateService(pet);

        var views = await service.ClearStandbyAsync(playerId, pet.Id);

        views.Single().IsStandby.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_DeletesOwnedPet()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var pet = PlayerPet.Capture(playerId, SlimeId, DateTimeOffset.UtcNow);
        var repository = new FakePlayerPetRepository(pet);
        var service = CreateService(repository);

        await service.ReleaseAsync(playerId, pet.Id);

        repository.StoredPets.Should().BeEmpty();
    }

    [Fact]
    public async Task ReleaseAsync_WithOtherPlayersPet_ThrowsKeyNotFoundException()
    {
        var otherPlayersPet = PlayerPet.Capture(new PlayerId(Guid.NewGuid()), SlimeId, DateTimeOffset.UtcNow);
        var repository = new FakePlayerPetRepository(otherPlayersPet);
        var service = CreateService(repository);

        var act = () => service.ReleaseAsync(new PlayerId(Guid.NewGuid()), otherPlayersPet.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        repository.StoredPets.Should().HaveCount(1);
    }

    [Fact]
    public async Task TrainAsync_DelegatesToExecutorAndMapsResult()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var trainedPet = new PlayerPet(
            PlayerPetId.New(),
            playerId,
            SlimeId,
            new PetBonusStatus(maxHp: 3),
            isStandby: false,
            capturedAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var executor = new FakePetTrainingExecutor(trainedPet);
        var service = new PetService(new FakePlayerPetRepository(), executor, new FakeQuestEnemyDefinitionRepository());

        var view = await service.TrainAsync(playerId, trainedPet.Id);

        executor.CallCount.Should().Be(1);
        view.BonusStatus.MaxHp.Should().Be(3);
        view.TotalStatus.MaxHp.Should().Be(103);
    }

    private static PetService CreateService(params PlayerPet[] pets)
    {
        return CreateService(new FakePlayerPetRepository(pets));
    }

    private static PetService CreateService(FakePlayerPetRepository repository)
    {
        return new PetService(repository, new FakePetTrainingExecutor(), new FakeQuestEnemyDefinitionRepository());
    }

    private sealed class FakePetTrainingExecutor(PlayerPet? result = null) : IPetTrainingExecutor
    {
        public int CallCount { get; private set; }

        public Task<PlayerPet> TrainAsync(PlayerId playerId, PlayerPetId petId)
        {
            CallCount++;
            return Task.FromResult(result ?? throw new KeyNotFoundException("ペットが見つかりません。"));
        }
    }

    private sealed class FakeQuestEnemyDefinitionRepository : IQuestEnemyDefinitionRepository
    {
        private static readonly QuestEnemyDefinition Definition = new(
            SlimeId,
            "Slime",
            level: 5,
            new Status(maxHp: 100, maxMp: 50, strength: 20, defense: 15, intelligence: 10, luck: 5, speed: 10),
            "/images/slime.png",
            EnemyAiType.Aggressive,
            moveIds: []);

        public Task<QuestEnemyDefinition?> GetAsync(QuestEnemyDefinitionId id)
            => Task.FromResult(Definition.Id == id ? Definition : null);

        public Task<IReadOnlyList<QuestEnemyDefinition>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<QuestEnemyDefinition>>([Definition]);
    }
}
