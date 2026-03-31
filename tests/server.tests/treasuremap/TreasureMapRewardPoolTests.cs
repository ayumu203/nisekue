using FluentAssertions;
using server.domain.treasuremap;
using server.domain.treasuremap.enums;
using Xunit;

namespace server.tests.treasuremap;

public class TreasureMapRewardPoolTests
{
    [Fact]
    public void Draw_WhenPredicateFiltersCandidates_DrawsOnlyFromFilteredEntries()
    {
        var pool = new TreasureMapRewardPool(
            new TreasureMapRewardPoolId(1),
            "Test Pool",
            [
                new TreasureMapRewardEntry(TreasureMapRewardType.Item, weight: 10, itemId: 4001, quantityMin: 1, quantityMax: 1),
                new TreasureMapRewardEntry(TreasureMapRewardType.Gold, weight: 10, goldAmount: 100)
            ]);

        var drawn = pool.Draw(new Random(1234), entry => entry.RewardType == TreasureMapRewardType.Gold);

        drawn.RewardType.Should().Be(TreasureMapRewardType.Gold);
        drawn.GoldAmount.Should().Be(100);
    }

    [Fact]
    public void HasFallbackEntry_WhenFallbackExistsForType_ReturnsTrue()
    {
        var pool = new TreasureMapRewardPool(
            new TreasureMapRewardPoolId(2),
            "Fallback Pool",
            [
                new TreasureMapRewardEntry(TreasureMapRewardType.Item, weight: 10, itemId: 4001, quantityMin: 1, quantityMax: 1),
                new TreasureMapRewardEntry(TreasureMapRewardType.Item, weight: 1, itemId: 4002, quantityMin: 1, quantityMax: 1, isFallback: true)
            ]);

        var hasFallback = pool.HasFallbackEntry(TreasureMapRewardType.Item);

        hasFallback.Should().BeTrue();
    }

    [Fact]
    public void Draw_WhenNoEntryMatchesPredicate_ThrowsInvalidOperationException()
    {
        var pool = new TreasureMapRewardPool(
            new TreasureMapRewardPoolId(3),
            "Empty Filter Pool",
            [new TreasureMapRewardEntry(TreasureMapRewardType.Experience, weight: 10, experienceAmount: 50)]);

        var act = () => pool.Draw(new Random(42), entry => entry.RewardType == TreasureMapRewardType.Equipment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No reward entry matches*");
    }
}
