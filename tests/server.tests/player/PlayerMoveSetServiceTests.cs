using FluentAssertions;
using server.application.player;
using server.domain.player;
using server.domain.move;
using Xunit;

namespace server.tests;

public class PlayerMoveSetServiceTests
{
    [Fact]
    public async Task UpdateAsync_WhenRequestedOrderIsValid_ReordersMoveSetAndNormalizesNulls()
    {
        var player = CreatePlayerWithMoves(101, 102, 103);
        var repository = new FakePlayerRepository(player);
        var service = new PlayerMoveSetService(repository);

        var updated = await service.UpdateAsync(player.Id, [103, 101, 102, null, null, null, null, null, null, null]);

        updated.MoveSet.Slots.Select(x => x?.Id).Should().Equal(103, 101, 102, null, null, null, null, null, null, null);
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WhenMoveIdIsUnknown_ThrowsInvalidOperationException()
    {
        var player = CreatePlayerWithMoves(101, 102);
        var repository = new FakePlayerRepository(player);
        var service = new PlayerMoveSetService(repository);

        var act = () => service.UpdateAsync(player.Id, [101, 999, null, null, null, null, null, null, null, null]);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenMoveIdIsDuplicated_ThrowsInvalidOperationException()
    {
        var player = CreatePlayerWithMoves(101, 102);
        var repository = new FakePlayerRepository(player);
        var service = new PlayerMoveSetService(repository);

        var act = () => service.UpdateAsync(player.Id, [101, 101, null, null, null, null, null, null, null, null]);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static Player CreatePlayerWithMoves(params int[] moveIds)
    {
        var slots = new MoveId?[MoveSet.MaxSlots];
        for (var i = 0; i < moveIds.Length; i++)
        {
            slots[i] = new MoveId(moveIds[i]);
        }

        return new Player(
            new PlayerId(Guid.NewGuid()),
            "Tester",
            level: 5,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(30, 10, 10, 5, 3, 3, 5),
            job: Job.Warrior,
            moveSet: new MoveSet(slots));
    }

    private sealed class FakePlayerRepository(Player player) : IPlayerRepository
    {
        public int SaveCount { get; private set; }

        public Task<Player?> GetPlayerAsync(PlayerId id)
        {
            return Task.FromResult<Player?>(player.Id == id ? player : null);
        }

        public Task<Player?> GetPlayerWithinLevelCapAsync(PlayerId id, int maxLevel)
        {
            return Task.FromResult<Player?>(player.Id == id && player.Level <= maxLevel ? player : null);
        }

        public Task<IReadOnlyList<Player>> GetPvpOpponentsAsync(PlayerId excludeId, int maxLevel, int? offset = null, int? limit = null)
        {
            IReadOnlyList<Player> result = player.Id != excludeId && player.Level <= maxLevel ? [player] : [];
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Player>> GetAllAsync(int? offset = null, int? limit = null)
        {
            return Task.FromResult<IReadOnlyList<Player>>([player]);
        }

        public Task<IReadOnlyList<Player>> GetPlayersAsync(IEnumerable<PlayerId> ids)
        {
            var idSet = ids.ToHashSet();
            IReadOnlyList<Player> result = idSet.Contains(player.Id) ? [player] : [];
            return Task.FromResult(result);
        }

        public Task<bool> UpdateNameAsync(PlayerId id, string name)
        {
            throw new NotSupportedException();
        }

        public Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(Player savedPlayer)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
