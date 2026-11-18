using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;
using server.infrastructure.quest;
using Xunit;

namespace server.tests;

public class DbMirrorCandidateRepositoryTests
{
    [Fact]
    public async Task GetPlayersWithMovesAsync_WhenPlayersHaveMoves_ReturnsPlayersWithMoveSets()
    {
        var databaseName = $"db-mirror-candidate-{Guid.NewGuid()}";
        var withMovesId = Guid.NewGuid();
        var withoutMovesId = Guid.NewGuid();
        await using (var dbContext = CreateDbContext(databaseName))
        {
            dbContext.Players.AddRange(
                CreatePlayerEntity(withMovesId, "WithMoves", level: 100),
                CreatePlayerEntity(withoutMovesId, "WithoutMoves", level: 90),
                CreatePlayerEntity(Guid.NewGuid(), "NotRequested", level: 80));
            dbContext.PlayerMoves.Add(new PlayerMoveEntity { PlayerId = withMovesId, MoveId1 = 101, MoveId2 = 205 });
            await dbContext.SaveChangesAsync();
        }

        var repository = new DbMirrorCandidateRepository(new TestDbContextFactory(databaseName));

        var players = await repository.GetPlayersWithMovesAsync([new PlayerId(withMovesId), new PlayerId(withoutMovesId)]);

        players.Select(x => x.Id.Value).Should().BeEquivalentTo([withMovesId, withoutMovesId]);
        players.Single(x => x.Id.Value == withMovesId).MoveSet.GetLearnedMoveIds()
            .Select(x => x.Id).Should().Equal(101, 205);
        players.Single(x => x.Id.Value == withoutMovesId).MoveSet.GetLearnedMoveIds().Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlayersWithMovesAsync_WhenIdsEmpty_ReturnsEmpty()
    {
        var repository = new DbMirrorCandidateRepository(new TestDbContextFactory($"db-mirror-candidate-{Guid.NewGuid()}"));

        var players = await repository.GetPlayersWithMovesAsync([]);

        players.Should().BeEmpty();
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
        public AppDbContext CreateDbContext() => DbMirrorCandidateRepositoryTests.CreateDbContext(databaseName);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
