using FluentAssertions;
using server.domain.player;
using server.infrastructure.move;
using Xunit;

namespace server.tests;

public class CsvJobMoveLearningRuleRepositoryTests
{
    [Fact]
    public void GetByJob_WhenFourthTierJobExists_ReturnsExpectedMoveIds()
    {
        var repository = new CsvJobMoveLearningRuleRepository();

        var rule = repository.GetByJob(Job.GreatThief);

        rule.MoveIds.Select(x => x.Id).Should().Equal([521, 422]);
    }
}
