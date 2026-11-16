using FluentAssertions;
using Microsoft.Data.Sqlite;
using server.domain.player;
using server.infrastructure.player;
using server.infrastructure.quest;
using Xunit;

namespace server.tests.quest;

public class DbMirrorCandidateRepositoryTests
{
    [Fact]
    public async Task GetTopPlayerIdsWithTiesAsync_IncludesPlayersTiedWithBoundary()
    {
        await using var connection = await OpenAsync();
        var top = CreatePlayer("Top", level: 50, rebirthCount: 1);
        var tiedA = CreatePlayer("TiedA", level: 100, rebirthCount: 0);
        var tiedB = CreatePlayer("TiedB", level: 100, rebirthCount: 0);
        var low = CreatePlayer("Low", level: 10, rebirthCount: 0);
        await SeedAsync(connection, top, tiedA, tiedB, low);
        var repository = new DbMirrorCandidateRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var ids = await repository.GetTopPlayerIdsWithTiesAsync(2);

        ids.Select(x => x.Value).Should().BeEquivalentTo(new[] { top.Id, tiedA.Id, tiedB.Id });
    }

    [Fact]
    public async Task GetTopPlayerIdsWithTiesAsync_WhenFewerPlayersThanCount_ReturnsAll()
    {
        await using var connection = await OpenAsync();
        var first = CreatePlayer("First", level: 50, rebirthCount: 0);
        var second = CreatePlayer("Second", level: 40, rebirthCount: 0);
        await SeedAsync(connection, first, second);
        var repository = new DbMirrorCandidateRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var ids = await repository.GetTopPlayerIdsWithTiesAsync(20);

        ids.Select(x => x.Value).Should().BeEquivalentTo(new[] { first.Id, second.Id });
    }

    [Fact]
    public async Task GetAllyPlayerIdsAsync_PrefersPlayersInLevelRangeThenNearestLevels()
    {
        await using var connection = await OpenAsync();
        var self = CreatePlayer("Self", level: 50, rebirthCount: 0);
        var inRange = CreatePlayer("InRange", level: 55, rebirthCount: 0);
        var near = CreatePlayer("Near", level: 65, rebirthCount: 0);
        var far = CreatePlayer("Far", level: 95, rebirthCount: 0);
        await SeedAsync(connection, self, inRange, near, far);
        var repository = new DbMirrorCandidateRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var ids = await repository.GetAllyPlayerIdsAsync(new PlayerId(self.Id), level: 50, levelRange: 10, count: 2);

        ids.Select(x => x.Value).Should().Equal(inRange.Id, near.Id);
    }

    [Fact]
    public async Task GetAllyPlayerIdsAsync_NeverReturnsExcludedPlayer()
    {
        await using var connection = await OpenAsync();
        var self = CreatePlayer("Self", level: 50, rebirthCount: 0);
        await SeedAsync(connection, self);
        var repository = new DbMirrorCandidateRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var ids = await repository.GetAllyPlayerIdsAsync(new PlayerId(self.Id), level: 50, levelRange: 10, count: 3);

        ids.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlayersWithMovesAsync_WhenPlayersHaveMoves_ReturnsPlayersWithMoveSets()
    {
        await using var connection = await OpenAsync();
        var withMoves = CreatePlayer("WithMoves", level: 100, rebirthCount: 0);
        var withoutMoves = CreatePlayer("WithoutMoves", level: 90, rebirthCount: 0);
        var notRequested = CreatePlayer("NotRequested", level: 80, rebirthCount: 0);
        await SeedAsync(connection, withMoves, withoutMoves, notRequested);
        await using (var dbContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            dbContext.PlayerMoves.Add(new PlayerMoveEntity { PlayerId = withMoves.Id, MoveId1 = 101, MoveId2 = 205 });
            await dbContext.SaveChangesAsync();
        }

        var repository = new DbMirrorCandidateRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var players = await repository.GetPlayersWithMovesAsync([new PlayerId(withMoves.Id), new PlayerId(withoutMoves.Id)]);

        players.Select(x => x.Id.Value).Should().BeEquivalentTo(new[] { withMoves.Id, withoutMoves.Id });
        players.Single(x => x.Id.Value == withMoves.Id).MoveSet.GetLearnedMoveIds()
            .Select(x => x.Id).Should().Equal(101, 205);
        players.Single(x => x.Id.Value == withoutMoves.Id).MoveSet.GetLearnedMoveIds().Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlayersWithMovesAsync_WhenIdsEmpty_ReturnsEmpty()
    {
        await using var connection = await OpenAsync();
        var repository = new DbMirrorCandidateRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var players = await repository.GetPlayersWithMovesAsync([]);

        players.Should().BeEmpty();
    }

    private static async Task<SqliteConnection> OpenAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var dbContext = TestHelpers.CreateSqliteDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        return connection;
    }

    private static async Task SeedAsync(SqliteConnection connection, params PlayerEntity[] players)
    {
        await using var dbContext = TestHelpers.CreateSqliteDbContext(connection);
        dbContext.Players.AddRange(players);
        await dbContext.SaveChangesAsync();
    }

    private static PlayerEntity CreatePlayer(string name, int level, int rebirthCount)
    {
        return new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Job = Job.Warrior,
            Level = level,
            Exp = 0,
            JobLevel = 1,
            JobExp = 0,
            Gold = 0,
            RebirthCount = rebirthCount,
            MaxHp = 10,
            MaxMp = 10,
            Strength = 1,
            Defense = 1,
            Intelligence = 1,
            Luck = 1,
            Speed = 1,
            TrainingBattleCount = 0
        };
    }
}
