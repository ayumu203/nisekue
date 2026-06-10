using FluentAssertions;
using server.domain.battle;
using server.domain.pet_battle;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.pet_battle;

public class PetBattlePartyMemberStateTests
{
    private static PetBattlePartyMemberState MakeAlive(int canActFromTurn = 1) =>
        new(PetBattleParticipantId.NewId(), 200, 100, isDead: false, canActFromTurn, ActionMode.Manual);

    private static PetBattlePartyMemberState MakeDead() =>
        new(PetBattleParticipantId.NewId(), 0, 0, isDead: true, 1, ActionMode.Manual);

    // --- CanAcceptManualCommand ---

    [Fact]
    public void CanAcceptManualCommand_WhenManualAliveAndTurnReached_ReturnsTrue()
    {
        var member = MakeAlive(canActFromTurn: 1);

        member.CanAcceptManualCommand(currentTurnNo: 1).Should().BeTrue();
    }

    [Fact]
    public void CanAcceptManualCommand_WhenDead_ReturnsFalse()
    {
        var member = MakeDead();

        member.CanAcceptManualCommand(currentTurnNo: 1).Should().BeFalse();
    }

    [Fact]
    public void CanAcceptManualCommand_WhenAutoAttackOnly_ReturnsFalse()
    {
        var member = new PetBattlePartyMemberState(
            PetBattleParticipantId.NewId(), 200, 100, isDead: false, 1, ActionMode.AutoAttackOnly);

        member.CanAcceptManualCommand(currentTurnNo: 1).Should().BeFalse();
    }

    [Fact]
    public void CanAcceptManualCommand_WhenTurnNotYetReached_ReturnsFalse()
    {
        var member = MakeAlive(canActFromTurn: 3);

        member.CanAcceptManualCommand(currentTurnNo: 2).Should().BeFalse();
    }

    [Fact]
    public void CanAcceptManualCommand_WhenExactlyAtCanActFromTurn_ReturnsTrue()
    {
        var member = MakeAlive(canActFromTurn: 3);

        member.CanAcceptManualCommand(currentTurnNo: 3).Should().BeTrue();
    }

    // --- ApplyBattleState ---

    [Fact]
    public void ApplyBattleState_WhenAliveStaysAlive_DoesNotChangeCanActFromTurn()
    {
        var member = MakeAlive(canActFromTurn: 1);
        var state = new BattleActorState(new BattleActorId(Guid.NewGuid()), 100, 50);

        member.ApplyBattleState(state, currentTurnNo: 2);

        member.IsDead.Should().BeFalse();
        member.CanActFromTurn.Should().Be(1);
    }

    [Fact]
    public void ApplyBattleState_WhenAliveAndHpDropsToZero_SetsDead()
    {
        var member = MakeAlive();
        var state = new BattleActorState(new BattleActorId(Guid.NewGuid()), 0, 0);

        member.ApplyBattleState(state, currentTurnNo: 1);

        member.IsDead.Should().BeTrue();
    }

    [Fact]
    public void ApplyBattleState_WhenRevived_SetsCanActFromTurnPlusOne()
    {
        var member = MakeDead();
        var state = new BattleActorState(new BattleActorId(Guid.NewGuid()), 100, 50);

        member.ApplyBattleState(state, currentTurnNo: 3);

        member.IsDead.Should().BeFalse();
        member.CanActFromTurn.Should().Be(4);
    }

    [Fact]
    public void ApplyBattleState_WhenDeadStaysDead_DoesNotChangeCanActFromTurn()
    {
        var member = MakeDead();
        var state = new BattleActorState(new BattleActorId(Guid.NewGuid()), 0, 0);

        member.ApplyBattleState(state, currentTurnNo: 5);

        member.IsDead.Should().BeTrue();
        member.CanActFromTurn.Should().Be(1);
    }
}
