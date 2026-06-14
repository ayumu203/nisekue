using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;
using Xunit;

namespace server.tests;

public class DbPlayerRebirthExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEligible_RebirthsAndRecordsPreviousStatusHistory()
    {
        var databaseName = $"rebirth-executor-{Guid.NewGuid()}";
        var playerId = Guid.NewGuid();

        await SeedPlayersAsync(
            databaseName,
            CreatePlayerEntity(playerId, "Hero", level: 120, gold: 150000, maxHp: 120, maxMp: 80, strength: 30, defense: 28, intelligence: 42, luck: 18, speed: 25));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var executor = new DbPlayerRebirthExecutor(
            new TestDbContextFactory(databaseName),
            new PlayerForUpdateLockService(),
            cache);

        var inheritedStatus = new Status(maxHp: 35, maxMp: 24, strength: 9, defense: 8, intelligence: 12, luck: 5, speed: 7);
        var result = await executor.ExecuteAsync(new PlayerId(playerId), _ => inheritedStatus);

        result.Level.Should().Be(1);
        result.RebirthCount.Should().Be(1);
        result.Gold.Should().Be(50000);
        result.Status.MaxHp.Should().Be(35);

        await using var verifyContext = CreateDbContext(databaseName);
        var history = verifyContext.PlayerRebirthStatusHistories.Single();
        history.PlayerId.Should().Be(playerId);
        history.RebirthCount.Should().Be(1);
        history.MaxHp.Should().Be(120);
        history.MaxMp.Should().Be(80);
        history.Strength.Should().Be(30);
        history.Defense.Should().Be(28);
        history.Intelligence.Should().Be(42);
        history.Luck.Should().Be(18);
        history.Speed.Should().Be(25);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRebirthedTwice_RecordsBothGenerations()
    {
        var databaseName = $"rebirth-executor-{Guid.NewGuid()}";
        var playerId = Guid.NewGuid();

        await SeedPlayersAsync(
            databaseName,
            CreatePlayerEntity(playerId, "Hero", level: 120, gold: 300000, maxHp: 120, maxMp: 80, strength: 30, defense: 28, intelligence: 42, luck: 18, speed: 25));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var executor = new DbPlayerRebirthExecutor(
            new TestDbContextFactory(databaseName),
            new PlayerForUpdateLockService(),
            cache);

        await executor.ExecuteAsync(new PlayerId(playerId), _ => new Status(maxHp: 100, maxMp: 100, strength: 100, defense: 100, intelligence: 100, luck: 100, speed: 100));

        // 2回目の転生条件を満たすようにレベルとGoldを再設定する
        await using (var prepContext = CreateDbContext(databaseName))
        {
            var entity = prepContext.Players.Single(x => x.Id == playerId);
            entity.Level = 150;
            entity.Gold = 200000;
            await prepContext.SaveChangesAsync();
        }

        await executor.ExecuteAsync(new PlayerId(playerId), _ => new Status(maxHp: 50, maxMp: 50, strength: 50, defense: 50, intelligence: 50, luck: 50, speed: 50));

        await using var verifyContext = CreateDbContext(databaseName);
        var histories = verifyContext.PlayerRebirthStatusHistories
            .Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.RebirthCount)
            .ToList();

        histories.Should().HaveCount(2);
        histories[0].RebirthCount.Should().Be(1);
        histories[0].MaxHp.Should().Be(120);
        histories[1].RebirthCount.Should().Be(2);
        histories[1].MaxHp.Should().Be(100);
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

    private static PlayerEntity CreatePlayerEntity(
        Guid id,
        string name,
        int level,
        int gold,
        int maxHp,
        int maxMp,
        int strength,
        int defense,
        int intelligence,
        int luck,
        int speed)
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
            Gold = gold,
            MaxHp = maxHp,
            MaxMp = maxMp,
            Strength = strength,
            Defense = defense,
            Intelligence = intelligence,
            Luck = luck,
            Speed = speed
        };
    }

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => DbPlayerRebirthExecutorTests.CreateDbContext(databaseName);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}