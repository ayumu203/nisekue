using FluentAssertions;
using server.application.debug;
using server.domain;
using server.domain.player;
using server.domain.treasuremap;
using server.domain.treasuremap.enums;
using Xunit;

namespace server.tests;

public class DebugWaitServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ResetCooldownsAsync_ClearsQuestAndPetBattleCooldowns()
    {
        var player = DebugTestPlayers.Create();
        player.SetQuestCooldownUntil(Now.AddMinutes(3));
        player.SetPetBattleCooldownUntil(Now.AddMinutes(1));
        var service = CreateService(player, expedition: null);

        await service.ResetCooldownsAsync(player.Id);

        player.QuestCooldownUntil.Should().BeNull();
        player.PetBattleCooldownUntil.Should().BeNull();
    }

    [Fact]
    public async Task CompleteTreasureMapAsync_WhenInProgress_MovesEndsAtToNow()
    {
        var player = DebugTestPlayers.Create();
        var expedition = CreateExpedition(player.Id, Now.AddMinutes(-10), Now.AddHours(1));
        var repository = new SingleExpeditionRepository(expedition);
        var service = new DebugWaitService(new SinglePlayerMutationService(player), repository);

        var result = await service.CompleteTreasureMapAsync(player.Id, Now);

        result.EndsAt.Should().Be(Now);
        result.Status.Should().Be(TreasureMapExpeditionStatus.InProgress);
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task CompleteTreasureMapAsync_WhenAlreadyEnded_DoesNotSave()
    {
        var player = DebugTestPlayers.Create();
        var expedition = CreateExpedition(player.Id, Now.AddHours(-2), Now.AddHours(-1));
        var repository = new SingleExpeditionRepository(expedition);
        var service = new DebugWaitService(new SinglePlayerMutationService(player), repository);

        var result = await service.CompleteTreasureMapAsync(player.Id, Now);

        result.EndsAt.Should().Be(Now.AddHours(-1));
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task CompleteTreasureMapAsync_WhenNoExpedition_Throws()
    {
        var player = DebugTestPlayers.Create();
        var service = CreateService(player, expedition: null);

        var act = () => service.CompleteTreasureMapAsync(player.Id, Now);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task CompleteTreasureMapAsync_WhenNotInProgress_Throws()
    {
        var player = DebugTestPlayers.Create();
        var expedition = CreateExpedition(player.Id, Now.AddHours(-2), Now.AddHours(-1));
        expedition.MarkFailed(Now.AddHours(-1));
        var service = CreateService(player, expedition);

        var act = () => service.CompleteTreasureMapAsync(player.Id, Now);

        await act.Should().ThrowAsync<DomainException>();
    }

    private static DebugWaitService CreateService(Player player, TreasureMapExpedition? expedition)
    {
        return new DebugWaitService(new SinglePlayerMutationService(player), new SingleExpeditionRepository(expedition));
    }

    private static TreasureMapExpedition CreateExpedition(PlayerId playerId, DateTimeOffset startedAt, DateTimeOffset endsAt)
    {
        return new TreasureMapExpedition(
            TreasureMapExpeditionId.New(),
            new TreasureMapId(1),
            playerId,
            startedAt,
            endsAt);
    }

    private sealed class SingleExpeditionRepository(TreasureMapExpedition? expedition) : ITreasureMapExpeditionRepository
    {
        public int SaveCount { get; private set; }

        public Task<TreasureMapExpedition?> GetAsync(TreasureMapExpeditionId id)
            => Task.FromResult(expedition?.Id == id ? expedition : null);

        public Task<TreasureMapExpedition?> GetCurrentByPlayerAsync(PlayerId playerId)
            => Task.FromResult(expedition);

        public Task<IReadOnlyList<TreasureMapExpedition>> ListByPlayerAsync(PlayerId playerId)
            => Task.FromResult<IReadOnlyList<TreasureMapExpedition>>(expedition is not null ? [expedition] : []);

        public Task SaveAsync(TreasureMapExpedition saved)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
