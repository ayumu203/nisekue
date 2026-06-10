using FluentAssertions;
using server.domain.pet_battle;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.pet_battle;

public class PetBattleTurnStateTests
{
    private static readonly DateTimeOffset Deadline = DateTimeOffset.UtcNow.AddSeconds(60);

    private static PetBattleSubmittedCommand MakeCommand(PetBattleParticipantId participantId, int turnNo) =>
        new(participantId, turnNo, ActionKind.NormalAttack, DateTimeOffset.UtcNow);

    [Fact]
    public void Submit_WhenValid_AddsToPendingCommands()
    {
        var state = new PetBattleTurnState(1, Deadline);
        var participantId = PetBattleParticipantId.NewId();

        state.Submit(MakeCommand(participantId, 1));

        state.PendingCommands.Should().ContainSingle(c => c.ParticipantId == participantId);
    }

    [Fact]
    public void Submit_WhenWrongTurnNo_ThrowsInvalidOperationException()
    {
        var state = new PetBattleTurnState(1, Deadline);
        var participantId = PetBattleParticipantId.NewId();

        var act = () => state.Submit(MakeCommand(participantId, turnNo: 2));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Submit_WhenDuplicateParticipant_ThrowsInvalidOperationException()
    {
        var state = new PetBattleTurnState(1, Deadline);
        var participantId = PetBattleParticipantId.NewId();
        state.Submit(MakeCommand(participantId, 1));

        var act = () => state.Submit(MakeCommand(participantId, 1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Submit_WhenTwoDifferentParticipants_AddsBoth()
    {
        var state = new PetBattleTurnState(1, Deadline);
        var p1 = PetBattleParticipantId.NewId();
        var p2 = PetBattleParticipantId.NewId();

        state.Submit(MakeCommand(p1, 1));
        state.Submit(MakeCommand(p2, 1));

        state.PendingCommands.Should().HaveCount(2);
    }

    [Fact]
    public void Advance_IncreasesTurnNoAndClearsCommands()
    {
        var state = new PetBattleTurnState(1, Deadline);
        var participantId = PetBattleParticipantId.NewId();
        state.Submit(MakeCommand(participantId, 1));
        var nextDeadline = Deadline.AddSeconds(60);

        state.Advance(nextDeadline);

        state.CurrentTurnNo.Should().Be(2);
        state.PendingCommands.Should().BeEmpty();
        state.LastResolvedTurnNo.Should().Be(1);
        state.ActionDeadlineAt.Should().Be(nextDeadline);
    }
}
