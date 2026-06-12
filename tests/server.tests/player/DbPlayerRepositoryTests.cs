using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;
using Xunit;

namespace server.tests;

public class DbPlayerRepositoryTests
{
    [Fact]
    public async Task GetPvpOpponentsAsync_WhenLevelOnePlayersExist_ExcludesThemFromResults()
    {
        var databaseName = $"db-player-repository-{Guid.NewGuid()}";
        var ownerId = Guid.NewGuid();
        var levelOneOpponentId = Guid.NewGuid();
        var levelTwoOpponentId = Guid.NewGuid();
        var levelFiveOpponentId = Guid.NewGuid();

        await SeedPlayersAsync(
            databaseName,
            CreatePlayerEntity(ownerId, "Owner", level: 10),
            CreatePlayerEntity(levelOneOpponentId, "Lv1", level: 1),
            CreatePlayerEntity(levelTwoOpponentId, "Lv2", level: 2),
            CreatePlayerEntity(levelFiveOpponentId, "Lv5", level: 5),
            CreatePlayerEntity(Guid.NewGuid(), "TooHigh", level: 11));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new SupabasePlayerRepository(new TestDbContextFactory(databaseName), cache);

        var opponents = await repository.GetPvpOpponentsAsync(new PlayerId(ownerId), maxLevel: 10);

        opponents.Select(x => x.Id.Value).Should().BeEquivalentTo([levelTwoOpponentId, levelFiveOpponentId]);
        opponents.Should().OnlyContain(x => x.Level >= 2);
    }

    [Fact]
    public async Task CountPvpOpponentsAsync_WhenLevelOnePlayersExist_DoesNotCountThem()
    {
        var databaseName = $"db-player-repository-{Guid.NewGuid()}";
        var ownerId = Guid.NewGuid();

        await SeedPlayersAsync(
            databaseName,
            CreatePlayerEntity(ownerId, "Owner", level: 10),
            CreatePlayerEntity(Guid.NewGuid(), "Lv1A", level: 1),
            CreatePlayerEntity(Guid.NewGuid(), "Lv1B", level: 1),
            CreatePlayerEntity(Guid.NewGuid(), "Lv2", level: 2),
            CreatePlayerEntity(Guid.NewGuid(), "Lv3", level: 3),
            CreatePlayerEntity(Guid.NewGuid(), "TooHigh", level: 15));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new SupabasePlayerRepository(new TestDbContextFactory(databaseName), cache);

        var count = await repository.CountPvpOpponentsAsync(new PlayerId(ownerId), maxLevel: 10);

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetPvpOpponentsAsync_WhenOwnerIsLevelOne_IncludesLevelOneOpponents()
    {
        var databaseName = $"db-player-repository-{Guid.NewGuid()}";
        var ownerId = Guid.NewGuid();
        var levelOneOpponentId = Guid.NewGuid();
        var levelTwoOpponentId = Guid.NewGuid();

        await SeedPlayersAsync(
            databaseName,
            CreatePlayerEntity(ownerId, "Owner", level: 1),
            CreatePlayerEntity(levelOneOpponentId, "Lv1", level: 1),
            CreatePlayerEntity(levelTwoOpponentId, "Lv2", level: 2));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new SupabasePlayerRepository(new TestDbContextFactory(databaseName), cache);

        var opponents = await repository.GetPvpOpponentsAsync(new PlayerId(ownerId), maxLevel: 1);
        var count = await repository.CountPvpOpponentsAsync(new PlayerId(ownerId), maxLevel: 1);

        opponents.Select(x => x.Id.Value).Should().BeEquivalentTo([levelOneOpponentId]);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetPvpOpponentsPageAsync_ReturnsOpponentsOrderedByLevelThenNameThenId()
    {
        var databaseName = $"db-player-repository-{Guid.NewGuid()}";
        var ownerId = Guid.NewGuid();
        var sameLevelLaterId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var sameLevelEarlierId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var levelTwoId = Guid.NewGuid();
        var levelFiveId = Guid.NewGuid();

        await SeedPlayersAsync(
            databaseName,
            CreatePlayerEntity(ownerId, "Owner", level: 10),
            CreatePlayerEntity(levelFiveId, "Aaron", level: 5),
            CreatePlayerEntity(sameLevelLaterId, "Alex", level: 3),
            CreatePlayerEntity(levelTwoId, "Zed", level: 2),
            CreatePlayerEntity(sameLevelEarlierId, "Alex", level: 3));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new SupabasePlayerRepository(new TestDbContextFactory(databaseName), cache);

        var (opponents, _) = await repository.GetPvpOpponentsPageAsync(new PlayerId(ownerId), maxLevel: 10);

        opponents.Select(x => x.Id.Value).Should().Equal(levelTwoId, sameLevelEarlierId, sameLevelLaterId, levelFiveId);
    }

    private static async Task SeedPlayersAsync(string databaseName, params PlayerEntity[] players)
    {
        await using var dbContext = CreateDbContext(databaseName);
        dbContext.Players.AddRange(players);
        await dbContext.SaveChangesAsync();
    }

    private static AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new AppDbContext(options);
    }

    private static PlayerEntity CreatePlayerEntity(Guid id, string name, int level)
    {
        return new PlayerEntity
        {
            Id = id,
            Name = name,
            Job = Job.Warrior,
            Level = level,
            Exp = 0,
            JobLevel = 1,
            JobExp = 0,
            Gold = 0,
            MaxHp = 10,
            MaxMp = 10,
            Strength = 10,
            Defense = 10,
            Intelligence = 10,
            Luck = 10,
            Speed = 10
        };
    }

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => DbPlayerRepositoryTests.CreateDbContext(databaseName);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
