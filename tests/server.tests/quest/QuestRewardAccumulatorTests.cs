using FluentAssertions;
using server.domain.quest;
using Xunit;

namespace server.tests.quest;

public class QuestRewardAccumulatorTests
{
    [Fact]
    public void AddExp_WhenSumExceedsIntMax_SaturatesAtIntMaxValue()
    {
        var accumulator = new QuestRewardAccumulator(exp: int.MaxValue - 10);

        accumulator.AddExp(1000);

        accumulator.Exp.Should().Be(int.MaxValue);
    }

    [Fact]
    public void AddGold_WhenSumExceedsIntMax_SaturatesAtIntMaxValue()
    {
        var accumulator = new QuestRewardAccumulator(gold: int.MaxValue - 10);

        accumulator.AddGold(1000);

        accumulator.Gold.Should().Be(int.MaxValue);
    }

    [Fact]
    public void AddExp_WhenSumWithinRange_AccumulatesNormally()
    {
        var accumulator = new QuestRewardAccumulator(exp: 100);

        accumulator.AddExp(250);

        accumulator.Exp.Should().Be(350);
    }
}
