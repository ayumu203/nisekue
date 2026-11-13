using FluentAssertions;
using Microsoft.Data.Sqlite;
using server.application.announcement;
using server.domain;
using server.infrastructure.announcement;
using Xunit;

namespace server.tests.announcement;

public class AnnouncementServiceTests
{
    [Fact]
    public async Task PostAsync_WhenDateIsOmitted_UsesTodayInJst()
    {
        await using var connection = await OpenConnectionAsync();
        var service = CreateService(connection);

        // UTC 15:30 は JST では翌日 0:30
        var now = new DateTimeOffset(2026, 11, 12, 15, 30, 0, TimeSpan.Zero);
        var announcement = await service.PostAsync(new PostAnnouncementCommand(null, null, "タイトル", "本文"), now);

        announcement.Date.Should().Be(new DateOnly(2026, 11, 13));
        announcement.Id.Value.Should().StartWith("2026-11-13-");
    }

    [Fact]
    public async Task PostAsync_WhenIdAlreadyExists_ThrowsConflict()
    {
        await using var connection = await OpenConnectionAsync();
        var service = CreateService(connection);
        var now = DateTimeOffset.UtcNow;

        await service.PostAsync(new PostAnnouncementCommand("same-id", new DateOnly(2026, 11, 13), "1件目", "本文"), now);
        var act = () => service.PostAsync(new PostAnnouncementCommand("same-id", new DateOnly(2026, 11, 13), "2件目", "本文"), now);

        await act.Should().ThrowAsync<AnnouncementIdConflictException>();
    }

    [Fact]
    public async Task PostAsync_WhenIdIsInvalid_ThrowsDomainException()
    {
        await using var connection = await OpenConnectionAsync();
        var service = CreateService(connection);

        var act = () => service.PostAsync(new PostAnnouncementCommand("Invalid ID", null, "タイトル", "本文"), DateTimeOffset.UtcNow);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task ListAsync_WhenMultipleAnnouncementsExist_ReturnsNewestDateFirstThenNewestCreatedFirst()
    {
        await using var connection = await OpenConnectionAsync();
        var service = CreateService(connection);
        var baseTime = new DateTimeOffset(2026, 11, 13, 0, 0, 0, TimeSpan.Zero);

        await service.PostAsync(new PostAnnouncementCommand("old", new DateOnly(2026, 8, 1), "古い", "本文"), baseTime);
        await service.PostAsync(new PostAnnouncementCommand("same-day-first", new DateOnly(2026, 11, 13), "同日1", "本文"), baseTime.AddMinutes(1));
        await service.PostAsync(new PostAnnouncementCommand("same-day-second", new DateOnly(2026, 11, 13), "同日2", "本文"), baseTime.AddMinutes(2));

        var result = await service.ListAsync();

        result.Select(x => x.Id.Value).Should().Equal("same-day-second", "same-day-first", "old");
    }

    [Fact]
    public async Task DeleteAsync_WhenIdExists_RemovesAnnouncementAndReturnsTrue()
    {
        await using var connection = await OpenConnectionAsync();
        var service = CreateService(connection);
        await service.PostAsync(new PostAnnouncementCommand("to-delete", null, "タイトル", "本文"), DateTimeOffset.UtcNow);

        var deleted = await service.DeleteAsync("to-delete");
        var result = await service.ListAsync();

        deleted.Should().BeTrue();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenIdDoesNotExist_ReturnsFalse()
    {
        await using var connection = await OpenConnectionAsync();
        var service = CreateService(connection);

        var deleted = await service.DeleteAsync("missing");

        deleted.Should().BeFalse();
    }

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = TestHelpers.CreateSqliteDbContext(connection);
        await context.Database.EnsureCreatedAsync();
        return connection;
    }

    private static AnnouncementService CreateService(SqliteConnection connection)
    {
        return new AnnouncementService(new DbAnnouncementRepository(new TestHelpers.SqliteDbContextFactory(connection)));
    }
}
