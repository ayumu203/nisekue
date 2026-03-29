using FluentAssertions;
using server.application.player;
using server.domain.player;
using Xunit;

namespace server.tests;

public class MarketListingCleanupServiceTests
{
    [Fact]
    public async Task DeleteExpiredAsync_WhenEquipmentListingExpired_DeletesEquipmentListingAndWritesLog()
    {
        var now = DateTimeOffset.UtcNow;
        var sellerId = new PlayerId(Guid.NewGuid());
        var equipment = CreateEquipment(sellerId, new EquipmentId(1001));
        var listing = new MarketListing(
            MarketListingId.New(),
            sellerId,
            equipment.Id,
            itemId: null,
            itemName: "銅の剣",
            flavorText: "テスト用の剣",
            quantity: 1,
            remainingQuantity: 1,
            unitPrice: 10,
            listedAt: now.AddDays(-16),
            expiresAt: now.AddMinutes(-1));
        var listingRepository = new FakeMarketListingRepository(listing);
        var equipmentRepository = new FakePlayerEquipmentRepository(equipment);
        var deletionLogRepository = new FakeItemDeletionLogRepository();
        var service = new MarketListingCleanupService(listingRepository, equipmentRepository, deletionLogRepository);

        var result = await service.DeleteExpiredAsync(now);

        result.DeletedListings.Should().Be(1);
        result.DeletedEquipments.Should().Be(1);
        result.DeletedItemQuantity.Should().Be(0);
        (await listingRepository.GetAsync(listing.Id)).Should().BeNull();
        (await equipmentRepository.GetAsync(equipment.Id)).Should().BeNull();
        deletionLogRepository.Logs.Should().ContainSingle();
        deletionLogRepository.Logs[0].ItemIdentifier.Should().Be("equipment:1001");
        deletionLogRepository.Logs[0].Reason.Should().Be("expired_listing_cleanup");
    }

    [Fact]
    public async Task DeleteExpiredAsync_WhenItemListingExpired_DeletesListingAndAggregatesDeletedQuantity()
    {
        var now = DateTimeOffset.UtcNow;
        var sellerId = new PlayerId(Guid.NewGuid());
        var listing = new MarketListing(
            MarketListingId.New(),
            sellerId,
            playerEquipmentId: null,
            itemId: new ItemId(3001),
            itemName: "体力の実",
            flavorText: "テスト用の実",
            quantity: 5,
            remainingQuantity: 3,
            unitPrice: 20,
            listedAt: now.AddDays(-16),
            expiresAt: now.AddMinutes(-1));
        var listingRepository = new FakeMarketListingRepository(listing);
        var equipmentRepository = new FakePlayerEquipmentRepository();
        var deletionLogRepository = new FakeItemDeletionLogRepository();
        var service = new MarketListingCleanupService(listingRepository, equipmentRepository, deletionLogRepository);

        var result = await service.DeleteExpiredAsync(now);

        result.DeletedListings.Should().Be(1);
        result.DeletedEquipments.Should().Be(0);
        result.DeletedItemQuantity.Should().Be(3);
        deletionLogRepository.Logs.Should().ContainSingle();
        deletionLogRepository.Logs[0].ItemIdentifier.Should().Be("item:3001");
        deletionLogRepository.Logs[0].Quantity.Should().Be(3);
    }

    private static PlayerEquipment CreateEquipment(PlayerId playerId, EquipmentId equipmentId)
    {
        return new PlayerEquipment(
            PlayerEquipmentId.New(),
            playerId,
            equipmentId,
            EquipmentType.Weapon,
            EquipmentStatus.Inventory,
            durability: 10,
            mastery: 0,
            acquiredAt: DateTimeOffset.UtcNow.AddDays(-20),
            updatedAt: DateTimeOffset.UtcNow.AddDays(-20));
    }

    private sealed class FakeMarketListingRepository(params MarketListing[] listings) : IMarketListingRepository
    {
        private readonly Dictionary<MarketListingId, MarketListing> listingsById = listings.ToDictionary(x => x.Id);

        public Task<MarketListing?> GetAsync(MarketListingId id)
            => Task.FromResult(listingsById.GetValueOrDefault(id));

        public Task<IReadOnlyList<MarketListing>> GetActiveAsync(DateTimeOffset now)
            => Task.FromResult((IReadOnlyList<MarketListing>)listingsById.Values.Where(x => !x.IsExpired(now) && !x.IsSoldOut).ToArray());

        public Task<IReadOnlyList<MarketListing>> GetBySellerAsync(PlayerId sellerId, DateTimeOffset now)
            => Task.FromResult((IReadOnlyList<MarketListing>)listingsById.Values
                .Where(x => x.SellerId == sellerId && !x.IsExpired(now) && !x.IsSoldOut)
                .ToArray());

        public Task<IReadOnlyList<MarketListing>> GetExpiredAsync(DateTimeOffset now)
            => Task.FromResult((IReadOnlyList<MarketListing>)listingsById.Values
                .Where(x => x.IsExpired(now) && !x.IsSoldOut)
                .ToArray());

        public Task SaveAsync(MarketListing listing)
        {
            listingsById[listing.Id] = listing;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(MarketListingId id)
        {
            listingsById.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlayerEquipmentRepository(params PlayerEquipment[] equipments) : IPlayerEquipmentRepository
    {
        private readonly Dictionary<PlayerEquipmentId, PlayerEquipment> equipmentsById = equipments.ToDictionary(x => x.Id);

        public Task<IReadOnlyList<PlayerEquipment>> GetByPlayerAsync(PlayerId playerId)
            => Task.FromResult((IReadOnlyList<PlayerEquipment>)equipmentsById.Values.Where(x => x.PlayerId == playerId).ToArray());

        public Task<PlayerEquipment?> GetAsync(PlayerEquipmentId id)
            => Task.FromResult(equipmentsById.GetValueOrDefault(id));

        public Task SaveAsync(IReadOnlyList<PlayerEquipment> playerEquipments)
        {
            foreach (var equipment in playerEquipments)
            {
                equipmentsById[equipment.Id] = equipment;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(PlayerEquipmentId id)
        {
            equipmentsById.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeItemDeletionLogRepository : IItemDeletionLogRepository
    {
        public List<ItemDeletionLog> Logs { get; } = [];

        public Task AddAsync(ItemDeletionLog log)
        {
            Logs.Add(log);
            return Task.CompletedTask;
        }
    }
}
