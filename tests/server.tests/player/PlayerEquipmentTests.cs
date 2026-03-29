using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerEquipmentTests
{
    [Fact]
    public void RepairDurability_WhenRecoveredValueWouldExceedMax_ClampsToMaxDurability()
    {
        var equipment = CreateEquipment(status: EquipmentStatus.Equipped, durability: 8);

        equipment.RepairDurability(10, maxDurability: 10, DateTimeOffset.UtcNow);

        equipment.Durability.Should().Be(10);
    }

    [Fact]
    public void RepairDurability_WhenEquipmentIsBroken_Throws()
    {
        var equipment = CreateEquipment(status: EquipmentStatus.Broken, durability: 0);

        var action = () => equipment.RepairDurability(1, maxDurability: 10, DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*破損した装備は修復対象にできません*");
    }

    [Fact]
    public void TransferOwnership_WhenEquipmentIsNotBroken_MovesItToInventory()
    {
        var equipment = CreateEquipment(status: EquipmentStatus.Equipped, durability: 5);
        var nextOwnerId = new PlayerId(Guid.NewGuid());

        equipment.TransferOwnership(nextOwnerId, DateTimeOffset.UtcNow);

        equipment.PlayerId.Should().Be(nextOwnerId);
        equipment.Status.Should().Be(EquipmentStatus.Inventory);
    }

    [Fact]
    public void TransferOwnership_WhenEquipmentIsBroken_KeepsBrokenStatus()
    {
        var equipment = CreateEquipment(status: EquipmentStatus.Broken, durability: 0);
        var nextOwnerId = new PlayerId(Guid.NewGuid());

        equipment.TransferOwnership(nextOwnerId, DateTimeOffset.UtcNow);

        equipment.PlayerId.Should().Be(nextOwnerId);
        equipment.Status.Should().Be(EquipmentStatus.Broken);
    }

    private static PlayerEquipment CreateEquipment(EquipmentStatus status, int durability)
    {
        return new PlayerEquipment(
            PlayerEquipmentId.New(),
            new PlayerId(Guid.NewGuid()),
            new EquipmentId(1001),
            EquipmentType.Weapon,
            status,
            durability,
            mastery: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
    }
}
