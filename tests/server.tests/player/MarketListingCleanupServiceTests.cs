using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using server.application.player;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;
using Xunit;

namespace server.tests;

public class MarketListingCleanupServiceTests
{
    [Fact]
    public async Task DeleteExpiredAsync_WhenEquipmentListingExpired_DeletesEquipmentListingAndWritesLog()
    {
        var databaseName = $"market-cleanup-{Guid.NewGuid()}";
        var now = DateTimeOffset.UtcNow;
        var sellerId = Guid.NewGuid();
        var playerEquipmentId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.MarketListings.Add(new MarketListingEntity
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                PlayerEquipmentId = playerEquipmentId,
                ItemName = "銅の剣",
                FlavorText = "テスト用の剣",
                Quantity = 1,
                RemainingQuantity = 1,
                UnitPrice = 10,
                ListedAt = now.AddDays(-16),
                ExpiresAt = now.AddMinutes(-1)
            });
            seedContext.PlayerEquipments.Add(new PlayerEquipmentEntity
            {
                Id = playerEquipmentId,
                PlayerId = sellerId,
                EquipmentId = 1001,
                EquipmentType = (int)EquipmentType.Weapon,
                EquipmentStatus = (int)EquipmentStatus.Inventory,
                Durability = 10,
                Mastery = 0,
                AcquiredAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-20)
            });
            await seedContext.SaveChangesAsync();
        }

        var service = new MarketListingCleanupService(new TestDbContextFactory(databaseName));

        var result = await service.DeleteExpiredAsync(now);

        result.DeletedListings.Should().Be(1);
        result.DeletedEquipments.Should().Be(1);
        result.DeletedItemQuantity.Should().Be(0);

        await using var verifyContext = CreateDbContext(databaseName);
        (await verifyContext.MarketListings.CountAsync()).Should().Be(0);
        (await verifyContext.PlayerEquipments.CountAsync()).Should().Be(0);
        verifyContext.ItemDeletionLogs.Should().ContainSingle();
        verifyContext.ItemDeletionLogs.Single().ItemIdentifier.Should().Be("equipment:1001");
        verifyContext.ItemDeletionLogs.Single().Reason.Should().Be("expired_listing_cleanup");
    }

    [Fact]
    public async Task DeleteExpiredAsync_WhenItemListingExpired_DeletesListingAndAggregatesDeletedQuantity()
    {
        var databaseName = $"market-cleanup-{Guid.NewGuid()}";
        var now = DateTimeOffset.UtcNow;
        var sellerId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.MarketListings.Add(new MarketListingEntity
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                ItemId = 3001,
                ItemName = "体力の実",
                FlavorText = "テスト用の実",
                Quantity = 5,
                RemainingQuantity = 3,
                UnitPrice = 20,
                ListedAt = now.AddDays(-16),
                ExpiresAt = now.AddMinutes(-1)
            });
            await seedContext.SaveChangesAsync();
        }

        var service = new MarketListingCleanupService(new TestDbContextFactory(databaseName));

        var result = await service.DeleteExpiredAsync(now);

        result.DeletedListings.Should().Be(1);
        result.DeletedEquipments.Should().Be(0);
        result.DeletedItemQuantity.Should().Be(3);

        await using var verifyContext = CreateDbContext(databaseName);
        (await verifyContext.MarketListings.CountAsync()).Should().Be(0);
        verifyContext.ItemDeletionLogs.Should().ContainSingle();
        verifyContext.ItemDeletionLogs.Single().ItemIdentifier.Should().Be("item:3001");
        verifyContext.ItemDeletionLogs.Single().Quantity.Should().Be(3);
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
        public AppDbContext CreateDbContext() => MarketListingCleanupServiceTests.CreateDbContext(databaseName);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(MarketListingCleanupServiceTests.CreateDbContext(databaseName));
    }
}
