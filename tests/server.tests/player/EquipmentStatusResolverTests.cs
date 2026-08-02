using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests;

public class EquipmentStatusResolverTests
{
    [Fact]
    public void BuildEffectiveStatus_WhenEquippedWeaponMasteryIsBelowTen_DoesNotAddMasteryBonus()
    {
        var resolver = new EquipmentStatusResolver();
        var baseStatus = new Status(20, 10, 5, 4, 3, 2, 1);
        var weapon = CreateWeapon(strength: 20);
        var playerWeapon = CreatePlayerEquipment(weapon.Id, EquipmentType.Weapon, mastery: 9);

        var actual = resolver.BuildEffectiveStatus(baseStatus, Job.Apprentice, [playerWeapon], [weapon]);

        actual.Strength.Should().Be(25);
    }

    [Fact]
    // 熟練度は隠しステータスであり、ステータス計算には影響しない。
    public void BuildEffectiveStatus_WhenWeaponHasMastery_DoesNotAffectStatus()
    {
        var resolver = new EquipmentStatusResolver();
        var baseStatus = new Status(20, 10, 5, 4, 3, 2, 1);
        var weapon = CreateWeapon(strength: 100);
        var playerWeapon = CreatePlayerEquipment(weapon.Id, EquipmentType.Weapon, mastery: 10);

        var actual = resolver.BuildEffectiveStatus(baseStatus, Job.Apprentice, [playerWeapon], [weapon]);

        actual.Strength.Should().Be(105);
    }

    [Fact]
    public void BuildEffectiveStatus_WhenEquipmentIsArmor_DoesNotApplyMastery()
    {
        var resolver = new EquipmentStatusResolver();
        var baseStatus = new Status(20, 10, 5, 4, 3, 2, 1);
        var armor = CreateArmor(defense: 100);
        var playerArmor = CreatePlayerEquipment(armor.Id, EquipmentType.Armor, mastery: 50);

        var actual = resolver.BuildEffectiveStatus(baseStatus, Job.Apprentice, [playerArmor], [armor]);

        actual.Defense.Should().Be(104);
    }

    [Fact]
    public void BuildEffectiveStatus_WhenWeaponHasPlusValue_AppliesPlusOnly()
    {
        var resolver = new EquipmentStatusResolver();
        var baseStatus = new Status(20, 10, 5, 4, 3, 2, 1);
        var weapon = CreateWeapon(strength: 100);
        var playerWeapon = CreatePlayerEquipment(weapon.Id, EquipmentType.Weapon, mastery: 10, plusValue: 50);

        var actual = resolver.BuildEffectiveStatus(baseStatus, Job.Apprentice, [playerWeapon], [weapon]);

        actual.Strength.Should().Be(155);
    }

    [Fact]
    public void BuildEffectiveStatus_WhenArmorHasPlusValue_AppliesPlus()
    {
        var resolver = new EquipmentStatusResolver();
        var baseStatus = new Status(20, 10, 5, 4, 3, 2, 1);
        var armor = CreateArmor(defense: 100);
        var playerArmor = CreatePlayerEquipment(armor.Id, EquipmentType.Armor, mastery: 0, plusValue: 25);

        var actual = resolver.BuildEffectiveStatus(baseStatus, Job.Apprentice, [playerArmor], [armor]);

        actual.Defense.Should().Be(129);
    }

    [Fact]
    public void BuildEffectiveStatus_WhenPlusValueIsZero_NoBonusApplied()
    {
        var resolver = new EquipmentStatusResolver();
        var baseStatus = new Status(20, 10, 5, 4, 3, 2, 1);
        var weapon = CreateWeapon(strength: 100);
        var playerWeapon = CreatePlayerEquipment(weapon.Id, EquipmentType.Weapon, mastery: 0, plusValue: 0);

        var actual = resolver.BuildEffectiveStatus(baseStatus, Job.Apprentice, [playerWeapon], [weapon]);

        actual.Strength.Should().Be(105);
    }

    private static Equipment CreateWeapon(int strength)
    {
        return new Equipment(
            new EquipmentId(1001),
            "テスト剣",
            "test",
            EquipmentType.Weapon,
            maxDurability: 10,
            masteryCap: 100,
            synthesisGoldCost: 10,
            new EquipmentStatusBonus(0, 0, strength, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice });
    }

    private static Equipment CreateArmor(int defense)
    {
        return new Equipment(
            new EquipmentId(2001),
            "テスト鎧",
            "test",
            EquipmentType.Armor,
            maxDurability: 10,
            masteryCap: 100,
            synthesisGoldCost: 10,
            new EquipmentStatusBonus(0, 0, 0, defense, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice });
    }

    private static PlayerEquipment CreatePlayerEquipment(EquipmentId equipmentId, EquipmentType type, int mastery, int plusValue = 0)
    {
        return new PlayerEquipment(
            PlayerEquipmentId.New(),
            new PlayerId(Guid.NewGuid()),
            equipmentId,
            type,
            EquipmentStatus.Equipped,
            durability: 10,
            mastery: mastery,
            plusValue: plusValue,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
    }
}
