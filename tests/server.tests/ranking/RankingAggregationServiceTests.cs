using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using server.application.ranking;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;
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

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => RankingAggregationServiceTests.CreateDbContext(databaseName);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(RankingAggregationServiceTests.CreateDbContext(databaseName));
    }

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
