using FluentAssertions;
using server.application.debug;
using server.domain.player;
using Xunit;

namespace server.tests;

public class DebugInventoryServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly PlayerId TestPlayerId = new(Guid.NewGuid());

    [Fact]
    public async Task GrantItemAsync_WhenNotOwned_CreatesStack()
    {
        var (service, stacks, _) = CreateService();

        var quantity = await service.GrantItemAsync(TestPlayerId, 3001, 5, Now);

        quantity.Should().Be(5);
        stacks.Saved.Should().ContainSingle(x => x.ItemId == new ItemId(3001) && x.Quantity == 5);
    }

    [Fact]
    public async Task GrantItemAsync_WhenOwned_AddsAndCapsAtMaxStack()
    {
        var (service, stacks, _) = CreateService();
        stacks.Saved.Add(new PlayerItemStack(PlayerItemStackId.New(), TestPlayerId, new ItemId(3001), 95, Now));

        var quantity = await service.GrantItemAsync(TestPlayerId, 3001, 10, Now);

        quantity.Should().Be(99);
        stacks.Saved.Should().ContainSingle().Which.Quantity.Should().Be(99);
    }

    [Fact]
    public async Task GrantItemAsync_WhenQuantityExceedsMaxStack_CapsNewStack()
    {
        var (service, stacks, _) = CreateService();

        await service.GrantItemAsync(TestPlayerId, 3001, 500, Now);

        stacks.Saved.Should().ContainSingle().Which.Quantity.Should().Be(99);
    }

    [Fact]
    public async Task GrantItemAsync_WhenItemDoesNotExist_ThrowsArgumentException()
    {
        var (service, _, _) = CreateService();

        var act = () => service.GrantItemAsync(TestPlayerId, 9999, 1, Now);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GrantItemAsync_WhenQuantityIsZero_ThrowsArgumentOutOfRangeException()
    {
        var (service, _, _) = CreateService();

        var act = () => service.GrantItemAsync(TestPlayerId, 3001, 0, Now);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GrantEquipmentAsync_AddsToInventoryWithMaxDurability()
    {
        var (service, _, equipments) = CreateService();

        var count = await service.GrantEquipmentAsync(TestPlayerId, 1001, 3, plusValue: 5, Now);

        count.Should().Be(3);
        equipments.Saved.Should().HaveCount(3);
        equipments.Saved.Should().OnlyContain(x =>
            x.EquipmentId == new EquipmentId(1001)
            && x.Status == EquipmentStatus.Inventory
            && x.Durability == 10
            && x.Mastery == 0
            && x.PlusValue == 5);
        equipments.Saved.Select(x => x.Id).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(21, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    public async Task GrantEquipmentAsync_WhenOutOfRange_ThrowsArgumentOutOfRangeException(int count, int plusValue)
    {
        var (service, _, _) = CreateService();

        var act = () => service.GrantEquipmentAsync(TestPlayerId, 1001, count, plusValue, Now);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GrantEquipmentAsync_WhenEquipmentDoesNotExist_ThrowsArgumentException()
    {
        var (service, _, _) = CreateService();

        var act = () => service.GrantEquipmentAsync(TestPlayerId, 9999, 1, 0, Now);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static (DebugInventoryService Service, FakeItemStackRepository Stacks, FakeEquipmentRepository Equipments) CreateService()
    {
        var stacks = new FakeItemStackRepository();
        var equipments = new FakeEquipmentRepository();
        var service = new DebugInventoryService(new FakeItemRepository(), new FakeEquipmentMasterRepository(), stacks, equipments);
        return (service, stacks, equipments);
    }

    private sealed class FakeItemRepository : IItemRepository
    {
        private readonly Item item = new(new ItemId(3001), "いのちのたね", "最大HPが1上がる", 99, ItemEffectType.StatBoost);

        public Task<Item?> GetAsync(ItemId id) => Task.FromResult(id == item.Id ? item : null);

        public Task<IReadOnlyList<Item>> GetAllAsync() => Task.FromResult<IReadOnlyList<Item>>([item]);
    }

    private sealed class FakeEquipmentMasterRepository : IEquipmentRepository
    {
        private readonly Equipment equipment = new(
            new EquipmentId(1001),
            "旅立ちの剣",
            "旅路の始まりを告げる剣。",
            EquipmentType.Weapon,
            10,
            20,
            5,
            new EquipmentStatusBonus(0, 0, 3, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice });

        public Task<Equipment?> GetAsync(EquipmentId id) => Task.FromResult(id == equipment.Id ? equipment : null);

        public Task<IReadOnlyList<Equipment>> GetAllAsync() => Task.FromResult<IReadOnlyList<Equipment>>([equipment]);
    }

    private sealed class FakeItemStackRepository : IPlayerItemStackRepository
    {
        public List<PlayerItemStack> Saved { get; } = [];

        public Task<IReadOnlyList<PlayerItemStack>> GetByPlayerAsync(PlayerId playerId)
            => Task.FromResult<IReadOnlyList<PlayerItemStack>>(Saved.Where(x => x.PlayerId == playerId).ToList());

        public Task<PlayerItemStack?> GetAsync(PlayerItemStackId id) => Task.FromResult(Saved.FirstOrDefault(x => x.Id == id));

        public Task SaveAsync(IReadOnlyList<PlayerItemStack> playerItemStacks)
        {
            foreach (var stack in playerItemStacks.Where(x => !Saved.Contains(x)))
            {
                Saved.Add(stack);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(PlayerItemStackId id) => throw new NotSupportedException();
    }

    private sealed class FakeEquipmentRepository : IPlayerEquipmentRepository
    {
        public List<PlayerEquipment> Saved { get; } = [];

        public Task<IReadOnlyList<PlayerEquipment>> GetByPlayerAsync(PlayerId playerId)
            => Task.FromResult<IReadOnlyList<PlayerEquipment>>(Saved.Where(x => x.PlayerId == playerId).ToList());

        public Task<IReadOnlyList<PlayerEquipment>> GetEquippedByPlayerAsync(PlayerId playerId) => throw new NotSupportedException();

        public Task<PlayerEquipment?> GetAsync(PlayerEquipmentId playerEquipmentId) => throw new NotSupportedException();

        public Task SaveAsync(IReadOnlyList<PlayerEquipment> playerEquipments)
        {
            foreach (var equipment in playerEquipments.Where(x => !Saved.Contains(x)))
            {
                Saved.Add(equipment);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(PlayerEquipmentId playerEquipmentId) => throw new NotSupportedException();
    }
}
