using FluentAssertions;
using server.domain.quest;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.quest;

public class QuestRunPartyMemberStateTests
{
    [Fact]
    public void RequestManualControl_WhenActionModeIsNotAutoAttackOnly_ThrowsInvalidOperationException()
    {
        var state = CreateState(actionMode: ActionMode.Manual);

        var act = () => state.RequestManualControl();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RequestManualControl_WhenActionModeIsAutoAttackOnly_SetsRequestedFlag()
    {
        var state = CreateState(actionMode: ActionMode.AutoAttackOnly);

        state.RequestManualControl();

        state.IsManualControlRequested.Should().BeTrue();
    }

    [Theory]
    [InlineData(true, false, ActionMode.Manual, false)]
    [InlineData(false, true, ActionMode.Manual, false)]
    [InlineData(false, false, ActionMode.AutoAttackOnly, false)]
    [InlineData(false, false, ActionMode.Manual, true)]
    public void IsContinuable_ReturnsExpectedValue(bool isDead, bool hasLeftQuest, ActionMode actionMode, bool expected)
    {
        var state = CreateState(isDead: isDead, actionMode: actionMode, hasLeftQuest: hasLeftQuest);

        state.IsContinuable().Should().Be(expected);
    }

    [Fact]
    public void CanAcceptManualCommand_WhenCanActFromTurnIsFuture_ReturnsFalse()
    {
        var state = CreateState(canActFromTurn: 3);

        state.CanAcceptManualCommand(currentTurnNo: 2).Should().BeFalse();
    }

    private static QuestRunPartyMemberState CreateState(
        bool isDead = false,
        ActionMode actionMode = ActionMode.Manual,
        int canActFromTurn = 1,
        bool hasLeftQuest = false)
    {
        return new QuestRunPartyMemberState(
            QuestParticipantId.New(),
            currentHp: isDead ? 0 : 10,
            currentMp: 5,
            isDead: isDead,
            canActFromTurn: canActFromTurn,
            actionMode: actionMode,
            hasLeftQuest: hasLeftQuest);
    }
}
