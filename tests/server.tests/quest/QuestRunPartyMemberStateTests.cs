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

    [Fact]
    public void ConsumePetSummon_IncrementsPetSummonsUsed()
    {
        var state = CreateState();

        state.ConsumePetSummon();

        state.PetSummonsUsed.Should().Be(1);
        state.HasRemainingPetSummons.Should().BeTrue();
    }

    [Fact]
    public void ConsumePetSummon_WhenLimitReached_ThrowsInvalidOperationException()
    {
        var state = CreateState(petSummonsUsed: 3);

        var act = () => state.ConsumePetSummon();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void HasRemainingPetSummons_ReturnsExpectedValue(int petSummonsUsed, bool expected)
    {
        var state = CreateState(petSummonsUsed: petSummonsUsed);

        state.HasRemainingPetSummons.Should().Be(expected);
    }

    private static QuestRunPartyMemberState CreateState(
        bool isDead = false,
        ActionMode actionMode = ActionMode.Manual,
        int canActFromTurn = 1,
        bool hasLeftQuest = false,
        int petSummonsUsed = 0)
    {
        return new QuestRunPartyMemberState(
            QuestParticipantId.New(),
            currentHp: isDead ? 0 : 10,
            currentMp: 5,
            isDead: isDead,
            canActFromTurn: canActFromTurn,
            actionMode: actionMode,
            hasLeftQuest: hasLeftQuest,
            petSummonsUsed: petSummonsUsed);
    }
}
