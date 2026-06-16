using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using server.application.ranking;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.pet_battle;
using server.infrastructure.player;
using server.infrastructure.ranking;
using Xunit;

namespace server.tests.ranking;

public class RankingAggregationServiceTests
{
    [Fact]
    public async Task RebuildAsync_WhenCombatRankScopedRankingIsBuilt_CreatesAllRankEntriesWithNullCombatRank()
    {
        var databaseName = $"ranking-aggregation-{Guid.NewGuid()}";
        var now = DateTimeOffset.UtcNow;

        await using (var seedContext = CreateDbContext(databaseName))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Players.AddRange(
                CreatePlayer("Alice", maxHp: 80, maxMp: 20, strength: 30, defense: 20, intelligence: 10, luck: 10, speed: 20, trainingBattleCount: 12),
                CreatePlayer("Bob", maxHp: 40, maxMp: 10, strength: 10, defense: 8, intelligence: 5, luck: 5, speed: 8, trainingBattleCount: 5));
            await seedContext.SaveChangesAsync();
        }

        var service = new RankingAggregationService(
            new TestDbContextFactory(databaseName),
            new CombatIndexCalculator(new StaticCombatIndexWeightRepository()),
            new CombatIndexRankEvaluator(new StaticCombatIndexRankThresholdRepository()));

        await service.RebuildAsync(now);

        await using var verifyContext = CreateDbContext(databaseName);
        var trainingAllRankRows = await verifyContext.RankingEntries
            .AsNoTracking()
            .Where(x => x.RankingType == RankingConstants.TrainingBattleByCombatRank && x.CombatIndexRank == null)
            .OrderBy(x => x.PeriodKind)
            .ThenBy(x => x.RankPosition)
            .ToListAsync();

        trainingAllRankRows.Should().HaveCount(6);
        trainingAllRankRows.Select(x => x.PeriodKind).Distinct().Should().BeEquivalentTo(["Total", "Weekly", "Daily"]);
        trainingAllRankRows.Should().OnlyContain(x => x.CombatIndexRank == null);
        trainingAllRankRows.Where(x => x.PeriodKind == "Total").Select(x => x.RankPosition).Should().Equal(1, 2);
    }

    [Fact]
    public async Task RebuildAsync_WhenMoreThan10SnapshotsExist_PrunesOldestAndKeepsLatest10PlusNewSnapshot()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        Guid oldestSnapshotId;
        await using (var seedContext = CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var player = CreatePlayer("Alice", maxHp: 80, maxMp: 20, strength: 30, defense: 20, intelligence: 10, luck: 10, speed: 20, trainingBattleCount: 5);
            seedContext.Players.Add(player);

            oldestSnapshotId = Guid.NewGuid();
            seedContext.RankingSnapshots.Add(new RankingSnapshotEntity
            {
                Id = oldestSnapshotId,
                SnapshotAt = baseTime,
                IntervalHours = 6,
                CreatedAt = baseTime
            });
            seedContext.RankingEntries.Add(new RankingEntryEntity
            {
                Id = Guid.NewGuid(),
                SnapshotId = oldestSnapshotId,
                RankingType = "test",
                PeriodKind = "Total",
                PlayerId = player.Id,
                RankPosition = 1,
                Score = 100,
                CreatedAt = baseTime
            });

            for (var i = 1; i <= 10; i++)
            {
                var snapshotAt = baseTime.AddHours(i * 6);
                seedContext.RankingSnapshots.Add(new RankingSnapshotEntity
                {
                    Id = Guid.NewGuid(),
                    SnapshotAt = snapshotAt,
                    IntervalHours = 6,
                    CreatedAt = snapshotAt
                });
            }

            await seedContext.SaveChangesAsync();
        }

        var now = baseTime.AddHours(72);
        var service = new RankingAggregationService(
            new SqliteDbContextFactory(connection),
            new CombatIndexCalculator(new StaticCombatIndexWeightRepository()),
            new CombatIndexRankEvaluator(new StaticCombatIndexRankThresholdRepository()));

        await service.RebuildAsync(now);

        await using var verifyContext = CreateSqliteDbContext(connection);
        var snapshots = await verifyContext.RankingSnapshots.AsNoTracking().ToListAsync();
        var orphanedEntries = await verifyContext.RankingEntries.AsNoTracking()
            .Where(x => x.SnapshotId == oldestSnapshotId)
            .ToListAsync();

        snapshots.Should().HaveCount(11);
        snapshots.Should().NotContain(x => x.Id == oldestSnapshotId);
        orphanedEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task RebuildAsync_WhenPetBattleStatsExist_CreatesTotalPetBattleRatingTopEntries()
    {
        var databaseName = $"ranking-aggregation-{Guid.NewGuid()}";
        var now = DateTimeOffset.UtcNow;

        var alice = CreatePlayer("Alice", maxHp: 80, maxMp: 20, strength: 30, defense: 20, intelligence: 10, luck: 10, speed: 20, trainingBattleCount: 12);
        var bob = CreatePlayer("Bob", maxHp: 40, maxMp: 10, strength: 10, defense: 8, intelligence: 5, luck: 5, speed: 8, trainingBattleCount: 5);
        var carol = CreatePlayer("Carol", maxHp: 60, maxMp: 15, strength: 20, defense: 15, intelligence: 8, luck: 7, speed: 12, trainingBattleCount: 3);

        await using (var seedContext = CreateDbContext(databaseName))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Players.AddRange(alice, bob, carol);
            seedContext.PlayerPetBattleStats.AddRange(
                new PlayerPetBattleStatsEntity
                {
                    PlayerId = alice.Id,
                    Rating = 1210,
                    Wins = 10,
                    Losses = 2,
                    TotalBattles = 12,
                    UpdatedAt = now
                },
                new PlayerPetBattleStatsEntity
                {
                    PlayerId = bob.Id,
                    Rating = 1080,
                    Wins = 4,
                    Losses = 4,
                    TotalBattles = 8,
                    UpdatedAt = now
                });
            await seedContext.SaveChangesAsync();
        }

        var service = new RankingAggregationService(
            new TestDbContextFactory(databaseName),
            new CombatIndexCalculator(new StaticCombatIndexWeightRepository()),
            new CombatIndexRankEvaluator(new StaticCombatIndexRankThresholdRepository()));

        await service.RebuildAsync(now);

        await using var verifyContext = CreateDbContext(databaseName);
        var petBattleRows = await verifyContext.RankingEntries
            .AsNoTracking()
            .Where(x => x.RankingType == RankingConstants.PetBattleRatingTop)
            .OrderBy(x => x.RankPosition)
            .ToListAsync();

        petBattleRows.Should().HaveCount(2);
        petBattleRows.Should().OnlyContain(x => x.PeriodKind == "Total");
        petBattleRows.Should().OnlyContain(x => x.CombatIndexRank == null);
        petBattleRows.Select(x => x.PlayerId).Should().Equal(alice.Id, bob.Id);
        petBattleRows.Select(x => x.Score).Should().Equal(1210, 1080);
    }

    [Fact]
    public async Task RebuildAsync_WhenPlayersHaveEndlessBestFloor_CreatesEndlessMaxFloorTopOrderedByFloor()
    {
        var databaseName = $"ranking-aggregation-{Guid.NewGuid()}";
        var now = DateTimeOffset.UtcNow;

        var alice = CreatePlayer("Alice", maxHp: 80, maxMp: 20, strength: 30, defense: 20, intelligence: 10, luck: 10, speed: 20, trainingBattleCount: 12);
        var bob = CreatePlayer("Bob", maxHp: 40, maxMp: 10, strength: 10, defense: 8, intelligence: 5, luck: 5, speed: 8, trainingBattleCount: 5);
        var carol = CreatePlayer("Carol", maxHp: 60, maxMp: 15, strength: 20, defense: 15, intelligence: 8, luck: 7, speed: 12, trainingBattleCount: 3);
        alice.EndlessBestFloor = 42;
        bob.EndlessBestFloor = 87;
        carol.EndlessBestFloor = 0;

        await using (var seedContext = CreateDbContext(databaseName))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Players.AddRange(alice, bob, carol);
            await seedContext.SaveChangesAsync();
        }

        var service = new RankingAggregationService(
            new TestDbContextFactory(databaseName),
            new CombatIndexCalculator(new StaticCombatIndexWeightRepository()),
            new CombatIndexRankEvaluator(new StaticCombatIndexRankThresholdRepository()));

        await service.RebuildAsync(now);

        await using var verifyContext = CreateDbContext(databaseName);
        var endlessRows = await verifyContext.RankingEntries
            .AsNoTracking()
            .Where(x => x.RankingType == RankingConstants.EndlessMaxFloorTop)
            .OrderBy(x => x.RankPosition)
            .ToListAsync();

        // 到達フロア降順。0 のプレイヤーは対象外。
        endlessRows.Should().HaveCount(2);
        endlessRows.Should().OnlyContain(x => x.PeriodKind == "Total");
        endlessRows.Should().OnlyContain(x => x.CombatIndexRank == null);
        endlessRows.Select(x => x.PlayerId).Should().Equal(bob.Id, alice.Id);
        endlessRows.Select(x => x.Score).Should().Equal(87, 42);
    }

    private static PlayerEntity CreatePlayer(
        string name,
        int maxHp,
        int maxMp,
        int strength,
        int defense,
        int intelligence,
        int luck,
        int speed,
        int trainingBattleCount)
    {
        return new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Job = Job.Warrior,
            Level = 10,
            Exp = 0,
            JobLevel = 5,
            JobExp = 0,
            Gold = 100,
            RebirthCount = 0,
            MaxHp = maxHp,
            MaxMp = maxMp,
            Strength = strength,
            Defense = defense,
            Intelligence = intelligence,
            Luck = luck,
            Speed = speed,
            TrainingBattleCount = trainingBattleCount
        };
    }

    private static AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static AppDbContext CreateSqliteDbContext(SqliteConnection connection) => TestHelpers.CreateSqliteDbContext(connection);

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => RankingAggregationServiceTests.CreateDbContext(databaseName);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(RankingAggregationServiceTests.CreateDbContext(databaseName));
    }

    private sealed class SqliteDbContextFactory(SqliteConnection connection) : TestHelpers.SqliteDbContextFactory(connection);

    private sealed class StaticCombatIndexWeightRepository : ICombatIndexWeightRepository
    {
        public IReadOnlyList<CombatIndexWeight> GetAll()
        {
            return
            [
                new CombatIndexWeight(RankingStatusKeys.MaxHp, 1.0),
                new CombatIndexWeight(RankingStatusKeys.MaxMp, 1.0),
                new CombatIndexWeight(RankingStatusKeys.Strength, 1.0),
                new CombatIndexWeight(RankingStatusKeys.Defense, 1.0),
                new CombatIndexWeight(RankingStatusKeys.Intelligence, 1.0),
                new CombatIndexWeight(RankingStatusKeys.Luck, 1.0),
                new CombatIndexWeight(RankingStatusKeys.Speed, 1.0)
            ];
        }
    }

    private sealed class StaticCombatIndexRankThresholdRepository : ICombatIndexRankThresholdRepository
    {
        public IReadOnlyList<StatusRankThreshold> GetAll()
        {
            return
            [
                new StatusRankThreshold("combatIndex", StatusRank.G, 100),
                new StatusRankThreshold("combatIndex", StatusRank.F, 200),
                new StatusRankThreshold("combatIndex", StatusRank.E, 300),
                new StatusRankThreshold("combatIndex", StatusRank.D, 400),
                new StatusRankThreshold("combatIndex", StatusRank.C, 500),
                new StatusRankThreshold("combatIndex", StatusRank.B, 700),
                new StatusRankThreshold("combatIndex", StatusRank.A, 900),
                new StatusRankThreshold("combatIndex", StatusRank.S, 1100),
                new StatusRankThreshold("combatIndex", StatusRank.SS, 1300),
                new StatusRankThreshold("combatIndex", StatusRank.SSS, int.MaxValue)
            ];
        }
    }
}
