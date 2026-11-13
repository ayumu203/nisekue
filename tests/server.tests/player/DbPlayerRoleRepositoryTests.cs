using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;
using Xunit;

namespace server.tests;

public class DbPlayerRoleRepositoryTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("Admin")]
    [InlineData(" admin ")]
    public async Task FindRoleAsync_WhenRoleIsAdmin_ReturnsAdmin(string role)
    {
        var (repository, playerId) = await CreateRepositoryWithPlayerAsync(role);

        var result = await repository.FindRoleAsync(new PlayerId(playerId));

        result.Should().Be(PlayerRole.Admin);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("player")]
    [InlineData("administrator")]
    public async Task FindRoleAsync_WhenRoleIsNotAdmin_ReturnsNull(string? role)
    {
        var (repository, playerId) = await CreateRepositoryWithPlayerAsync(role);

        var result = await repository.FindRoleAsync(new PlayerId(playerId));

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindRoleAsync_WhenPlayerDoesNotExist_ReturnsNull()
    {
        var databaseName = $"player-role-repo-{Guid.NewGuid()}";
        var repository = new DbPlayerRoleRepository(new TestDbContextFactory(databaseName));

        var result = await repository.FindRoleAsync(new PlayerId(Guid.NewGuid()));

        result.Should().BeNull();
    }

    private static async Task<(DbPlayerRoleRepository Repository, Guid PlayerId)> CreateRepositoryWithPlayerAsync(string? role)
    {
        var databaseName = $"player-role-repo-{Guid.NewGuid()}";
        var playerId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.Players.Add(new PlayerEntity
            {
                Id = playerId,
                Name = "tester",
                Level = 1,
                JobLevel = 1,
                MaxHp = 1,
                Role = role
            });
            await seedContext.SaveChangesAsync();
        }

        return (new DbPlayerRoleRepository(new TestDbContextFactory(databaseName)), playerId);
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
        public AppDbContext CreateDbContext() => DbPlayerRoleRepositoryTests.CreateDbContext(databaseName);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
