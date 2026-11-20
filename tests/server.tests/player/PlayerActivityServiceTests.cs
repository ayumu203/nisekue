using FluentAssertions;
using Microsoft.Data.Sqlite;
using server.application.player;
using server.infrastructure.player;
using Xunit;

namespace server.tests.player;

public class PlayerActivityServiceTests
{
    [Fact]
    public async Task RecordAsync_WhenLastActiveAtIsNull_SetsNow()
    {
        var now = new DateTimeOffset(2026, 11, 20, 6, 0, 0, TimeSpan.Zero);
        var lastActiveAt = await RecordAndReadAsync(previousLastActiveAt: null, now);

        lastActiveAt.Should().Be(now);
    }

    [Fact]
    public async Task RecordAsync_WhenRecordIntervalHasPassed_UpdatesToNow()
    {
        var now = new DateTimeOffset(2026, 11, 20, 6, 0, 0, TimeSpan.Zero);
        var previous = now - PlayerActivityService.RecordInterval - TimeSpan.FromSeconds(1);

        var lastActiveAt = await RecordAndReadAsync(previous, now);

        lastActiveAt.Should().Be(now);
    }

    [Fact]
    public async Task RecordAsync_WithinRecordInterval_KeepsPreviousValue()
    {
        var now = new DateTimeOffset(2026, 11, 20, 6, 0, 0, TimeSpan.Zero);
        var previous = now - TimeSpan.FromSeconds(30);

        var lastActiveAt = await RecordAndReadAsync(previous, now);

        lastActiveAt.Should().Be(previous);
    }

    [Fact]
    public void RecordInterval_IsShorterThanActiveWindow()
    {
        PlayerActivityService.RecordInterval.Should().BeLessThan(PlayerActivityService.ActiveWindow);
    }

    private static async Task<DateTimeOffset?> RecordAndReadAsync(DateTimeOffset? previousLastActiveAt, DateTimeOffset now)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var playerId = Guid.NewGuid();

        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Players.Add(new PlayerEntity
            {
                Id = playerId,
                Name = "tester",
                LastActiveAt = previousLastActiveAt
            });
            await seedContext.SaveChangesAsync();
        }

        await using (var context = TestHelpers.CreateSqliteDbContext(connection))
        {
            var service = new PlayerActivityService(context);
            await service.RecordAsync(playerId, now);
        }

        await using var readContext = TestHelpers.CreateSqliteDbContext(connection);
        var player = await readContext.Players.FindAsync(playerId);
        return player!.LastActiveAt;
    }
}
