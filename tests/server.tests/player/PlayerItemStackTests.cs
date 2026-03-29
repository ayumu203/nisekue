using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerItemStackTests
{
    [Fact]
    public void AddQuantity_WhenTotalIsBelowMaxStack_IncreasesQuantity()
    {
        var stack = CreateStack(quantity: 10);
        var now = DateTimeOffset.UtcNow;

        stack.AddQuantity(5, maxStack: 99, now);

        stack.Quantity.Should().Be(15);
        stack.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void AddQuantity_WhenTotalExceedsMaxStack_ClampsToMaxStack()
    {
        var stack = CreateStack(quantity: 95);

        stack.AddQuantity(10, maxStack: 99, DateTimeOffset.UtcNow);

        stack.Quantity.Should().Be(99);
    }

    [Fact]
    public void ConsumeQuantity_WhenQuantityIsSufficient_DecreasesQuantity()
    {
        var stack = CreateStack(quantity: 10);
        var now = DateTimeOffset.UtcNow;

        stack.ConsumeQuantity(4, now);

        stack.Quantity.Should().Be(6);
        stack.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void ConsumeQuantity_WhenQuantityIsInsufficient_Throws()
    {
        var stack = CreateStack(quantity: 3);

        var action = () => stack.ConsumeQuantity(4, DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*所持数を超えて消費できません*");
    }

    private static PlayerItemStack CreateStack(int quantity)
    {
        return new PlayerItemStack(
            PlayerItemStackId.New(),
            new PlayerId(Guid.NewGuid()),
            new ItemId(3001),
            quantity,
            DateTimeOffset.UtcNow);
    }
}
