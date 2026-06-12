using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using server.application.player;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;
using Xunit;

namespace server.tests;

public class MarketPurchaseServiceTests
{
    [Fact]
    public async Task PurchaseAsync_WhenBuyingItemListing_CreatesBuyerStackAndUpdatesTradeState()
    {
        var databaseName = $"market-purchase-{Guid.NewGuid()}";
        var now = DateTimeOffset.UtcNow;
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var listingId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Players.Add(CreatePlayerEntity(buyerId, "buyer", gold: 100));
            seedContext.Players.Add(CreatePlayerEntity(sellerId, "seller", gold: 20));
            seedContext.MarketListings.Add(new MarketListingEntity
            {
                Id = listingId,
                SellerId = sellerId,
                ItemId = 3001,
                ItemName = "古い体力の実",
                FlavorText = "テスト用",
                Quantity = 5,
                RemainingQuantity = 5,
                UnitPrice = 15,
                ListedAt = now.AddMinutes(-5),
                ExpiresAt = now.AddDays(1)
            });
            await seedContext.SaveChangesAsync();
        }

        var service = new MarketPurchaseService(
            new TestDbContextFactory(databaseName),
            new PlayerForUpdateLockService(),
            new TestItemRepository(new Item(new ItemId(3001), "いのちのたね", "HP+1", 99, ItemEffectType.StatBoost)));

        var result = await service.PurchaseAsync(new PlayerId(buyerId), new MarketListingId(listingId), 2);

        result.BuyerId.Should().Be(buyerId);
        result.SellerId.Should().Be(sellerId);
        result.TotalPrice.Should().Be(30);
        result.BuyerGold.Should().Be(70);

        await using var verifyContext = CreateDbContext(databaseName);
        verifyContext.PlayerItemStacks.Should().ContainSingle();
        verifyContext.PlayerItemStacks.Single().PlayerId.Should().Be(buyerId);
        verifyContext.PlayerItemStacks.Single().ItemId.Should().Be(3001);
        verifyContext.PlayerItemStacks.Single().Quantity.Should().Be(2);
        verifyContext.Players.Single(x => x.Id == buyerId).Gold.Should().Be(70);
        verifyContext.Players.Single(x => x.Id == sellerId).Gold.Should().Be(50);
        verifyContext.MarketListings.Single().RemainingQuantity.Should().Be(3);
        verifyContext.MarketTradeHistories.Should().ContainSingle();
        verifyContext.MarketTradeHistories.Single().BuyerId.Should().Be(buyerId);
        verifyContext.MarketTradeHistories.Single().SellerId.Should().Be(sellerId);
        verifyContext.MarketTradeHistories.Single().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task PurchaseAsync_WhenBuyerCannotAfford_DoesNotPersistPartialUpdates()
    {
        var databaseName = $"market-purchase-{Guid.NewGuid()}";
        var now = DateTimeOffset.UtcNow;
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var listingId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Players.Add(CreatePlayerEntity(buyerId, "buyer", gold: 10));
            seedContext.Players.Add(CreatePlayerEntity(sellerId, "seller", gold: 20));
            seedContext.MarketListings.Add(new MarketListingEntity
            {
                Id = listingId,
                SellerId = sellerId,
                ItemId = 3001,
                ItemName = "古い体力の実",
                FlavorText = "テスト用",
                Quantity = 1,
                RemainingQuantity = 1,
                UnitPrice = 50,
                ListedAt = now.AddMinutes(-5),
                ExpiresAt = now.AddDays(1)
            });
            await seedContext.SaveChangesAsync();
        }

        var service = new MarketPurchaseService(
            new TestDbContextFactory(databaseName),
            new PlayerForUpdateLockService(),
            new TestItemRepository(new Item(new ItemId(3001), "いのちのたね", "HP+1", 99, ItemEffectType.StatBoost)));

        var action = () => service.PurchaseAsync(new PlayerId(buyerId), new MarketListingId(listingId), 1);

        var exception = await Assert.ThrowsAsync<MarketPurchaseFailedException>(action);
        exception.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        exception.Message.Should().Contain("Gold");

        await using var verifyContext = CreateDbContext(databaseName);
        verifyContext.PlayerItemStacks.Should().BeEmpty();
        verifyContext.MarketTradeHistories.Should().BeEmpty();
        verifyContext.MarketListings.Single().RemainingQuantity.Should().Be(1);
        verifyContext.Players.Single(x => x.Id == buyerId).Gold.Should().Be(10);
        verifyContext.Players.Single(x => x.Id == sellerId).Gold.Should().Be(20);
    }

    [Fact]
    public async Task PurchaseAsync_WhenExistingStackWouldExceedMaxStack_ReturnsUnprocessableEntityAndKeepsState()
    {
        var databaseName = $"market-purchase-{Guid.NewGuid()}";
        var now = DateTimeOffset.UtcNow;
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var listingId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Players.Add(CreatePlayerEntity(buyerId, "buyer", gold: 100));
            seedContext.Players.Add(CreatePlayerEntity(sellerId, "seller", gold: 20));
            seedContext.PlayerItemStacks.Add(new PlayerItemStackEntity
            {
                Id = Guid.NewGuid(),
                PlayerId = buyerId,
                ItemId = 3001,
                Quantity = 98,
                UpdatedAt = now
            });
            seedContext.MarketListings.Add(new MarketListingEntity
            {
                Id = listingId,
                SellerId = sellerId,
                ItemId = 3001,
                ItemName = "古い体力の実",
                FlavorText = "テスト用",
                Quantity = 5,
                RemainingQuantity = 5,
                UnitPrice = 10,
                ListedAt = now.AddMinutes(-5),
                ExpiresAt = now.AddDays(1)
            });
            await seedContext.SaveChangesAsync();
        }

        var service = new MarketPurchaseService(
            new TestDbContextFactory(databaseName),
            new PlayerForUpdateLockService(),
            new TestItemRepository(new Item(new ItemId(3001), "いのちのたね", "HP+1", 99, ItemEffectType.StatBoost)));

        var action = () => service.PurchaseAsync(new PlayerId(buyerId), new MarketListingId(listingId), 2);

        var exception = await Assert.ThrowsAsync<MarketPurchaseFailedException>(action);
        exception.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        exception.Message.Should().Contain("スタック上限");

        await using var verifyContext = CreateDbContext(databaseName);
        verifyContext.PlayerItemStacks.Single().Quantity.Should().Be(98);
        verifyContext.MarketTradeHistories.Should().BeEmpty();
        verifyContext.MarketListings.Single().RemainingQuantity.Should().Be(5);
        verifyContext.Players.Single(x => x.Id == buyerId).Gold.Should().Be(100);
        verifyContext.Players.Single(x => x.Id == sellerId).Gold.Should().Be(20);
    }

    [Fact]
    public async Task PurchaseAsync_WhenBuyingEquipmentListing_TransfersOwnershipAndClosesListing()
    {
        var databaseName = $"market-purchase-{Guid.NewGuid()}";
        var now = DateTimeOffset.UtcNow;
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var playerEquipmentId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Players.Add(CreatePlayerEntity(buyerId, "buyer", gold: 100));
            seedContext.Players.Add(CreatePlayerEntity(sellerId, "seller", gold: 0));
            seedContext.PlayerEquipments.Add(new PlayerEquipmentEntity
            {
                Id = playerEquipmentId,
                PlayerId = sellerId,
                EquipmentId = 1001,
                EquipmentType = (int)EquipmentType.Weapon,
                EquipmentStatus = (int)EquipmentStatus.Inventory,
                Durability = 10,
                Mastery = 0,
                PlusValue = 0,
                AcquiredAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            });
            seedContext.MarketListings.Add(new MarketListingEntity
            {
                Id = listingId,
                SellerId = sellerId,
                PlayerEquipmentId = playerEquipmentId,
                ItemName = "銅の剣",
                FlavorText = "テスト用",
                Quantity = 1,
                RemainingQuantity = 1,
                UnitPrice = 40,
                ListedAt = now.AddMinutes(-5),
                ExpiresAt = now.AddDays(1)
            });
            await seedContext.SaveChangesAsync();
        }

        var service = new MarketPurchaseService(
            new TestDbContextFactory(databaseName),
            new PlayerForUpdateLockService(),
            new TestItemRepository());

        var result = await service.PurchaseAsync(new PlayerId(buyerId), new MarketListingId(listingId), 1);

        result.TotalPrice.Should().Be(40);
        await using var verifyContext = CreateDbContext(databaseName);
        verifyContext.MarketListings.Should().BeEmpty();
        verifyContext.PlayerEquipments.Single().PlayerId.Should().Be(buyerId);
        verifyContext.Players.Single(x => x.Id == buyerId).Gold.Should().Be(60);
        verifyContext.Players.Single(x => x.Id == sellerId).Gold.Should().Be(40);
        verifyContext.MarketTradeHistories.Should().ContainSingle();
        verifyContext.MarketTradeHistories.Single().ItemIdentifier.Should().Be($"equipment:{playerEquipmentId}");
    }

    private static PlayerEntity CreatePlayerEntity(Guid id, string name, int gold)
    {
        return new PlayerEntity
        {
            Id = id,
            Name = name,
            Job = Job.Apprentice,
            Level = 1,
            Exp = 0,
            JobLevel = 1,
            JobExp = 0,
            Gold = gold,
            MaxHp = 10,
            MaxMp = 0,
            Strength = 0,
            Defense = 0,
            Intelligence = 0,
            Luck = 0,
            Speed = 0
        };
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
        public AppDbContext CreateDbContext() => MarketPurchaseServiceTests.CreateDbContext(databaseName);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(MarketPurchaseServiceTests.CreateDbContext(databaseName));
    }

    private sealed class TestItemRepository(params Item[] items) : IItemRepository
    {
        private readonly IReadOnlyDictionary<ItemId, Item> itemsById = items.ToDictionary(x => x.Id);

        public Task<Item?> GetAsync(ItemId id)
        {
            itemsById.TryGetValue(id, out var item);
            return Task.FromResult(item);
        }

        public Task<IReadOnlyList<Item>> GetAllAsync()
            => Task.FromResult((IReadOnlyList<Item>)itemsById.Values.ToArray());
    }
}
