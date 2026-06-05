using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerEquipmentTests
{
    [Fact]
    public void Synthesize_WhenSameEquipment_IncreasesPlusValue()
    {
        var target = CreateEquipment(equipmentId: 1001, plusValue: 0);
        var source = CreateEquipment(equipmentId: 1001, plusValue: 0);

        target.Synthesize(source, synthesisGoldCost: 10, DateTimeOffset.UtcNow);

        target.PlusValue.Should().Be(1);
    }

    [Fact]
    public void Synthesize_WhenAtMaxPlusValue_Throws()
    {
        var target = CreateEquipment(equipmentId: 1001, plusValue: PlayerEquipment.MaxPlusValue);
        var source = CreateEquipment(equipmentId: 1001, plusValue: 0);

        var action = () => target.Synthesize(source, synthesisGoldCost: 10, DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*プラス値が上限に達している*");
    }

    [Fact]
    public void Synthesize_WhenDifferentEquipment_Throws()
    {
        var target = CreateEquipment(equipmentId: 1001, plusValue: 0);
        var source = CreateEquipment(equipmentId: 1002, plusValue: 0);

        var action = () => target.Synthesize(source, synthesisGoldCost: 10, DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*同一装備でしか合成はできません*");
    }

    [Fact]
    public void Synthesize_WhenSameInstance_Throws()
    {
        var equipment = CreateEquipment(equipmentId: 1001, plusValue: 0);

        var action = () => equipment.Synthesize(equipment, synthesisGoldCost: 10, DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*同じ装備個体は合成できません*");
    }

    [Fact]
    public void Synthesize_WhenSourceIsBroken_Throws()
    {
        var target = CreateEquipment(equipmentId: 1001, plusValue: 0);
        var source = CreateEquipment(equipmentId: 1001, plusValue: 0, status: EquipmentStatus.Broken, durability: 0);

        var action = () => target.Synthesize(source, synthesisGoldCost: 10, DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*破損した装備は合成素材にできません*");
    }

    [Fact]
    public void Synthesize_WhenTargetIsBroken_Throws()
    {
        var target = CreateEquipment(equipmentId: 1001, plusValue: 0, status: EquipmentStatus.Broken, durability: 0);
        var source = CreateEquipment(equipmentId: 1001, plusValue: 0);

        var action = () => target.Synthesize(source, synthesisGoldCost: 10, DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*破損した装備は合成対象にできません*");
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

    private static PlayerEquipment CreateEquipment(
        EquipmentStatus status = EquipmentStatus.Equipped,
        int durability = 10,
        int equipmentId = 1001,
        int plusValue = 0)
    {
        return new PlayerEquipment(
            PlayerEquipmentId.New(),
            new PlayerId(Guid.NewGuid()),
            new EquipmentId(equipmentId),
            EquipmentType.Weapon,
            status,
            durability,
            mastery: 0,
            plusValue: plusValue,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
    }
}
