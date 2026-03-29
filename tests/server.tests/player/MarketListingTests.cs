using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests;

public class MarketListingTests
{
    [Fact]
    public void Purchase_WhenRemainingQuantityIsEnough_DecreasesRemainingQuantity()
    {
        var listing = CreateListing(quantity: 5, remainingQuantity: 5, expiresAt: DateTimeOffset.UtcNow.AddDays(15));

        listing.Purchase(3);

        listing.RemainingQuantity.Should().Be(2);
        listing.IsSoldOut.Should().BeFalse();
    }

    [Fact]
    public void Purchase_WhenRemainingQuantityBecomesZero_MarksSoldOut()
    {
        var listing = CreateListing(quantity: 1, remainingQuantity: 1, expiresAt: DateTimeOffset.UtcNow.AddDays(15));

        listing.Purchase(1);

        listing.RemainingQuantity.Should().Be(0);
        listing.IsSoldOut.Should().BeTrue();
    }

    [Fact]
    public void Purchase_WhenRequestedQuantityExceedsRemaining_Throws()
    {
        var listing = CreateListing(quantity: 2, remainingQuantity: 2, expiresAt: DateTimeOffset.UtcNow.AddDays(15));

        var action = () => listing.Purchase(3);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*残数を超えて購入できません*");
    }

    [Fact]
    public void IsExpired_WhenNowReachedExpiry_ReturnsTrue()
    {
        var expiresAt = DateTimeOffset.UtcNow;
        var listing = CreateListing(quantity: 1, remainingQuantity: 1, expiresAt: expiresAt);

        listing.IsExpired(expiresAt).Should().BeTrue();
    }

    private static MarketListing CreateListing(int quantity, int remainingQuantity, DateTimeOffset expiresAt)
    {
        var listedAt = expiresAt.AddDays(-15);
        return new MarketListing(
            MarketListingId.New(),
            new PlayerId(Guid.NewGuid()),
            playerEquipmentId: null,
            itemId: new ItemId(3001),
            itemName: "体力の実",
            flavorText: "基礎体力を高める実。",
            quantity: quantity,
            remainingQuantity: remainingQuantity,
            unitPrice: 10,
            listedAt: listedAt,
            expiresAt: expiresAt);
    }
}
