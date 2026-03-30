using FluentAssertions;
using server.infrastructure.player;
using Xunit;

namespace server.tests;

public class CsvItemRepositoryTests
{
    [Fact]
    public async Task GetAsync_WhenPercentStatBoostItemExists_ReturnsPercentDefinition()
    {
        var repository = new CsvItemRepository();

        var item = await repository.GetAsync(new(3005));

        item.Should().NotBeNull();
        item!.Name.Should().Be("生命樹の雫");
        item.StatusBonusPercent.Should().NotBeNull();
        item.StatusBonusPercent!.MaxHpPercent.Should().Be(1);
        item.StatusBonusPercent.MaxMpPercent.Should().Be(0);
        item.StatusBonus!.MaxHp.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_WhenFlatStatBoostItemExists_ReturnsFlatDefinition()
    {
        var repository = new CsvItemRepository();

        var item = await repository.GetAsync(new(3001));

        item.Should().NotBeNull();
        item!.Name.Should().Be("命脈の種");
        item.StatusBonus.Should().NotBeNull();
        item.StatusBonus!.MaxHp.Should().Be(1);
        item.StatusBonusPercent.Should().NotBeNull();
        item.StatusBonusPercent!.MaxHpPercent.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_WhenHighTierFlatStatBoostItemExists_ReturnsExpectedDefinition()
    {
        var repository = new CsvItemRepository();

        var item = await repository.GetAsync(new(3051));

        item.Should().NotBeNull();
        item!.Name.Should().Be("疾風の神珠");
        item.StatusBonus.Should().NotBeNull();
        item.StatusBonus!.Speed.Should().Be(10);
        item.StatusBonusPercent.Should().NotBeNull();
        item.StatusBonusPercent!.SpeedPercent.Should().Be(0);
    }
}
