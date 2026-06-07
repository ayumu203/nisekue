using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests.player;

public class EquipmentTests
{
    // ----- CanEquip: 直接指定された職業 -----

    [Theory]
    [InlineData(Job.Mage)]
    [InlineData(Job.FireMage)]
    [InlineData(Job.WaterMage)]
    [InlineData(Job.WindMage)]
    [InlineData(Job.GrandCaster)]
    [InlineData(Job.Archmage)]
    public void CanEquip_WhenJobIsExplicitlyListed_ReturnsTrue(Job job)
    {
        var equipment = CreateMageWeapon();

        equipment.CanEquip(job).Should().BeTrue();
    }

    [Theory]
    [InlineData(Job.Warrior)]
    [InlineData(Job.Guardian)]
    [InlineData(Job.Priest)]
    [InlineData(Job.Ranger)]
    public void CanEquip_WhenUnrelatedBaseJob_ReturnsFalse(Job job)
    {
        var equipment = CreateMageWeapon();

        equipment.CanEquip(job).Should().BeFalse();
    }

    // ----- CanEquip: 5次職の上位職継承 -----

    [Fact]
    public void CanEquip_WhenBushin_CanEquipWarriorLineWeapon()
    {
        // 戦鬼の大剣: Warrior|OniWarrior|SwordMaster|GrandWarrior|Shogun
        var equipment = CreateEquipment([Job.Warrior, Job.OniWarrior, Job.SwordMaster, Job.GrandWarrior, Job.Shogun]);

        // Bushin は GrandWarrior → OniWarrior/SwordMaster → Warrior の上位職
        equipment.CanEquip(Job.Bushin).Should().BeTrue();
    }

    [Fact]
    public void CanEquip_WhenSeikaiou_CanEquipMageLineWeapon()
    {
        // 星導の魔杖: Mage|FireMage|WaterMage|WindMage|GrandCaster|Archmage
        var equipment = CreateMageWeapon();

        // Seikaiou は GrandCaster → FireMage/WaterMage/WindMage → Mage の上位職
        equipment.CanEquip(Job.Seikaiou).Should().BeTrue();
    }

    [Fact]
    public void CanEquip_WhenSeikaiou_CanEquipGrandCasterOnlyWeapon()
    {
        // 元素王の大杖: GrandCaster のみ
        var equipment = CreateEquipment([Job.GrandCaster]);

        equipment.CanEquip(Job.Seikaiou).Should().BeTrue();
    }

    [Fact]
    public void CanEquip_WhenMatouou_CanEquipGreatThiefLineWeapon()
    {
        // 災煙双刃: GreatThief のみ
        var equipment = CreateEquipment([Job.GreatThief]);

        // Matouou は GreatThief の上位職
        equipment.CanEquip(Job.Matouou).Should().BeTrue();
    }

    [Fact]
    public void CanEquip_WhenMatouou_CanEquipRangerLineWeapon()
    {
        // 狩人王の長弓: Ranger|Sniper|TrapMaster|GrandRanger|GreatThief
        var equipment = CreateEquipment([Job.Ranger, Job.Sniper, Job.TrapMaster, Job.GrandRanger, Job.GreatThief]);

        // Matouou → GreatThief → Warrior/WindMage/GrandRanger → (Sniper/TrapMaster) → Ranger
        equipment.CanEquip(Job.Matouou).Should().BeTrue();
    }

    [Fact]
    public void CanEquip_WhenBushin_CannotEquipMageLineWeapon()
    {
        var equipment = CreateMageWeapon();

        // Bushin は戦士系。魔法系装備は装備できない
        equipment.CanEquip(Job.Bushin).Should().BeFalse();
    }

    [Fact]
    public void CanEquip_WhenSeikaiou_CannotEquipWarriorLineWeapon()
    {
        var equipment = CreateEquipment([Job.Warrior, Job.OniWarrior, Job.SwordMaster, Job.GrandWarrior, Job.Shogun]);

        // Seikaiou は魔法系。戦士系装備は装備できない
        equipment.CanEquip(Job.Seikaiou).Should().BeFalse();
    }

    // ----- CanEquip: 4次職の上位職継承 -----

    [Fact]
    public void CanEquip_WhenShogun_CanEquipGrandWarriorOnlyArmor()
    {
        // 覇王戦装: GrandWarrior|Shogun
        var equipment = CreateEquipment([Job.GrandWarrior, Job.Shogun]);

        equipment.CanEquip(Job.Shogun).Should().BeTrue();
    }

    [Fact]
    public void CanEquip_WhenShogun_CanEquipGrandGuardOnlyArmor()
    {
        // 城塞聖甲: GrandGuard|Shogun
        var equipment = CreateEquipment([Job.GrandGuard, Job.Shogun]);

        equipment.CanEquip(Job.Shogun).Should().BeTrue();
    }

    [Fact]
    public void CanEquip_WhenArchmage_CanEquipGrandCasterOnlyArmor()
    {
        // 元素王法衣: GrandCaster|Archmage
        var equipment = CreateEquipment([Job.GrandCaster, Job.Archmage]);

        equipment.CanEquip(Job.Archmage).Should().BeTrue();
    }

    // ----- CanEquip: 全職業装備 -----

    [Theory]
    [InlineData(Job.Apprentice)]
    [InlineData(Job.Warrior)]
    [InlineData(Job.Mage)]
    [InlineData(Job.GrandWarrior)]
    [InlineData(Job.Archmage)]
    [InlineData(Job.Bushin)]
    [InlineData(Job.Seikaiou)]
    [InlineData(Job.Matouou)]
    public void CanEquip_WhenAllJobsEquipment_AllJobsCanEquip(Job job)
    {
        var allJobs = Enum.GetValues<Job>().ToHashSet();
        var equipment = CreateEquipment(allJobs);

        equipment.CanEquip(job).Should().BeTrue();
    }

    // ----- PlayerEquipment.Equip: 上位職での装備 -----

    [Fact]
    public void Equip_WhenBushinEquipsMageWeapon_Throws()
    {
        var mageWeapon = CreateMageWeapon();
        var playerEquipment = CreatePlayerEquipment(mageWeapon.Id, EquipmentType.Weapon);

        var action = () => playerEquipment.Equip(mageWeapon, Job.Bushin, DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*現在の職業ではこの装備を装備できません*");
    }

    [Fact]
    public void Equip_WhenSeikaiouEquipsMageWeapon_Succeeds()
    {
        var mageWeapon = CreateMageWeapon();
        var playerEquipment = CreatePlayerEquipment(mageWeapon.Id, EquipmentType.Weapon);

        var action = () => playerEquipment.Equip(mageWeapon, Job.Seikaiou, DateTimeOffset.UtcNow);

        action.Should().NotThrow();
        playerEquipment.Status.Should().Be(EquipmentStatus.Equipped);
    }

    [Fact]
    public void Equip_WhenBushinEquipsWarriorWeapon_Succeeds()
    {
        var warriorWeapon = CreateEquipment([Job.Warrior, Job.OniWarrior, Job.SwordMaster, Job.GrandWarrior, Job.Shogun]);
        var playerEquipment = CreatePlayerEquipment(warriorWeapon.Id, EquipmentType.Weapon);

        var action = () => playerEquipment.Equip(warriorWeapon, Job.Bushin, DateTimeOffset.UtcNow);

        action.Should().NotThrow();
        playerEquipment.Status.Should().Be(EquipmentStatus.Equipped);
    }

    [Fact]
    public void Equip_WhenMatououEquipsGreatThiefWeapon_Succeeds()
    {
        var greatThiefWeapon = CreateEquipment([Job.GreatThief]);
        var playerEquipment = CreatePlayerEquipment(greatThiefWeapon.Id, EquipmentType.Weapon);

        var action = () => playerEquipment.Equip(greatThiefWeapon, Job.Matouou, DateTimeOffset.UtcNow);

        action.Should().NotThrow();
        playerEquipment.Status.Should().Be(EquipmentStatus.Equipped);
    }

    // ----- ヘルパー -----

    // 星導の魔杖相当: Mage|FireMage|WaterMage|WindMage|GrandCaster|Archmage
    private static Equipment CreateMageWeapon() =>
        CreateEquipment([Job.Mage, Job.FireMage, Job.WaterMage, Job.WindMage, Job.GrandCaster, Job.Archmage]);

    private static Equipment CreateEquipment(IEnumerable<Job> equippableJobs)
    {
        return new Equipment(
            new EquipmentId(1001),
            "テスト装備",
            string.Empty,
            EquipmentType.Weapon,
            maxDurability: 10,
            masteryCap: 50,
            synthesisGoldCost: 10,
            new EquipmentStatusBonus(0, 0, 0, 0, 0, 0, 0),
            equippableJobs.ToHashSet());
    }

    private static PlayerEquipment CreatePlayerEquipment(EquipmentId equipmentId, EquipmentType type)
    {
        return new PlayerEquipment(
            PlayerEquipmentId.New(),
            new PlayerId(Guid.NewGuid()),
            equipmentId,
            type,
            EquipmentStatus.Inventory,
            durability: 10,
            mastery: 0,
            plusValue: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
    }
}
