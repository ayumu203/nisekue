using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;
using Xunit;

namespace server.tests;

public class DbPlayerRebirthHistoryRepositoryTests
{
    [Fact]
    public async Task GetByPlayerAsync_ReturnsOnlyTargetPlayerOrderedByRebirthCount()
    {
        var databaseName = $"rebirth-history-repo-{Guid.NewGuid()}";
        var playerId = Guid.NewGuid();
        var otherPlayerId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.PlayerRebirthStatusHistories.AddRange(
                CreateHistoryEntity(playerId, rebirthCount: 2, maxHp: 200),
                CreateHistoryEntity(playerId, rebirthCount: 1, maxHp: 100),
                CreateHistoryEntity(otherPlayerId, rebirthCount: 1, maxHp: 999));
            await seedContext.SaveChangesAsync();
        }

        var repository = new DbPlayerRebirthHistoryRepository(new TestDbContextFactory(databaseName));

        var histories = await repository.GetByPlayerAsync(new PlayerId(playerId));

        histories.Should().HaveCount(2);
        histories.Select(x => x.RebirthCount).Should().Equal(1, 2);
        histories[0].Status.MaxHp.Should().Be(100);
        histories[1].Status.MaxHp.Should().Be(200);
        histories.Should().OnlyContain(x => x.PlayerId.Value == playerId);
    }

    [Fact]
    public async Task GetByPlayerAsync_WhenNoHistory_ReturnsEmpty()
    {
        var databaseName = $"rebirth-history-repo-{Guid.NewGuid()}";
        var repository = new DbPlayerRebirthHistoryRepository(new TestDbContextFactory(databaseName));

        var histories = await repository.GetByPlayerAsync(new PlayerId(Guid.NewGuid()));

        histories.Should().BeEmpty();
    }

    private static PlayerRebirthStatusHistoryEntity CreateHistoryEntity(Guid playerId, int rebirthCount, int maxHp)
    {
        return new PlayerRebirthStatusHistoryEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            RebirthCount = rebirthCount,
            MaxHp = maxHp,
            MaxMp = 50,
            Strength = 10,
            Defense = 10,
            Intelligence = 10,
            Luck = 10,
            Speed = 10,
            RebirthedAt = DateTimeOffset.UtcNow
        };
    }

    private static AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new AppDbContext(options);
    }

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => DbPlayerRebirthHistoryRepositoryTests.CreateDbContext(databaseName);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}