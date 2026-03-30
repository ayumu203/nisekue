using FluentAssertions;
using server.application.player;
using server.domain.player;
using Xunit;

namespace server.tests;

public class ItemStatBoostServiceTests
{
    private readonly ItemStatBoostService service = new();

    [Fact]
    public void Apply_WhenPercentBoostItemIsUsedMultipleTimes_RecalculatesFromUpdatedStatusEachTime()
    {
        var item = new Item(
            new ItemId(4001),
            "生命樹の若枝",
            "最大HPが2%上がる",
            maxStack: 99,
            effectType: ItemEffectType.StatBoost,
            statusBonusPercent: new StatusBonusPercent(2, 0, 0, 0, 0, 0, 0));
        var currentStatus = new Status(101, 50, 20, 20, 20, 20, 20);

        var updated = service.Apply(currentStatus, item, quantity: 2);

        updated.MaxHp.Should().Be(107);
        updated.MaxMp.Should().Be(50);
        updated.Strength.Should().Be(20);
        updated.Accuracy.Should().Be(100);
    }

    [Fact]
    public void Apply_WhenFlatAndPercentBoostAreBothDefined_AddsBothEffects()
    {
        var item = new Item(
            new ItemId(4002),
            "賢者の秘薬",
            "知力が伸びる",
            maxStack: 99,
            effectType: ItemEffectType.StatBoost,
            statusBonus: new StatusBonus(0, 0, 0, 0, 2, 0, 0),
            statusBonusPercent: new StatusBonusPercent(0, 0, 0, 0, 5, 0, 0));
        var currentStatus = new Status(100, 40, 10, 10, 41, 10, 10);

        var updated = service.Apply(currentStatus, item, quantity: 1);

        updated.Intelligence.Should().Be(46);
    }
}
