using FluentAssertions;
using server.domain.quest;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.quest;

public class QuestTurnStateTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenTurnNumberIsZeroOrLess_ThrowsArgumentOutOfRangeException(int turnNo)
    {
        var act = () => new QuestTurnState(turnNo, DateTimeOffset.UtcNow.AddSeconds(30));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Submit_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        var state = new QuestTurnState(1, DateTimeOffset.UtcNow.AddSeconds(30));

        var act = () => state.Submit(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Submit_WhenValidCommand_AddsPendingCommand()
    {
        var state = new QuestTurnState(1, DateTimeOffset.UtcNow.AddSeconds(30));
        var command = new QuestSubmittedCommand(
            QuestParticipantId.New(),
            turnNo: 1,
            ActionKind.Wait,
            DateTimeOffset.UtcNow);

        state.Submit(command);

        state.PendingCommands.Should().ContainSingle().Which.Should().BeSameAs(command);
    }

    [Fact]
    public void Submit_WhenTurnNumberDoesNotMatch_ThrowsInvalidOperationException()
    {
        var state = new QuestTurnState(1, DateTimeOffset.UtcNow.AddSeconds(30));
        var command = new QuestSubmittedCommand(
            QuestParticipantId.New(),
            turnNo: 2,
            ActionKind.Wait,
            DateTimeOffset.UtcNow);

        var act = () => state.Submit(command);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Submit_WhenSameParticipantSubmitsTwice_ThrowsInvalidOperationException()
    {
        var participantId = QuestParticipantId.New();
        var state = new QuestTurnState(1, DateTimeOffset.UtcNow.AddSeconds(30));
        state.Submit(new QuestSubmittedCommand(participantId, 1, ActionKind.Wait, DateTimeOffset.UtcNow));

        var act = () => state.Submit(new QuestSubmittedCommand(participantId, 1, ActionKind.Guard, DateTimeOffset.UtcNow));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Advance_WhenCalled_IncrementsTurnAndClearsPendingCommands()
    {
        var state = new QuestTurnState(1, DateTimeOffset.UtcNow.AddSeconds(30));
        state.Submit(new QuestSubmittedCommand(QuestParticipantId.New(), 1, ActionKind.Wait, DateTimeOffset.UtcNow));

        state.Advance(DateTimeOffset.UtcNow.AddSeconds(60));

        state.CurrentTurnNo.Should().Be(2);
        state.LastResolvedTurnNo.Should().Be(1);
        state.PendingCommands.Should().BeEmpty();
    }

    [Fact]
    public void Advance_WhenCalled_UpdatesActionDeadline()
    {
        var initialDeadline = DateTimeOffset.UtcNow.AddSeconds(30);
        var nextDeadline = DateTimeOffset.UtcNow.AddSeconds(60);
        var state = new QuestTurnState(1, initialDeadline);

        state.Advance(nextDeadline);

        state.ActionDeadlineAt.Should().Be(nextDeadline);
    }
}
