using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.pet_battle;
using server.domain.pet_battle.enums;
using server.domain.player;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.pet_battle;

public class PetBattleRunTests
{
    private static readonly PlayerId OwnerId = new(Guid.NewGuid());
    private static readonly PlayerId OpponentId = new(Guid.NewGuid());
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateTimeOffset Deadline = Now.AddSeconds(60);

    // --- SubmitCommand ---

    [Fact]
    public void SubmitCommand_WhenValid_AddsPendingCommand()
    {
        var run = CreateRun();
        var member = run.OwnerMemberStates[0];
        var command = MakeCommand(member.ParticipantId, 1);

        run.SubmitCommand(member.ParticipantId, command, Now);

        run.TurnState.PendingCommands.Should().ContainSingle(c => c.ParticipantId == member.ParticipantId);
    }

    [Fact]
    public void SubmitCommand_WhenDeadlineExceeded_ThrowsInvalidOperationException()
    {
        var run = CreateRun();
        var member = run.OwnerMemberStates[0];
        var command = MakeCommand(member.ParticipantId, 1);

        var act = () => run.SubmitCommand(member.ParticipantId, command, Deadline.AddSeconds(1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SubmitCommand_WhenParticipantIdMismatch_ThrowsInvalidOperationException()
    {
        var run = CreateRun();
        var other = new PetBattleParticipantId(Guid.NewGuid());
        var member = run.OwnerMemberStates[0];
        var command = MakeCommand(other, 1);

        var act = () => run.SubmitCommand(member.ParticipantId, command, Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SubmitCommand_WhenRunIsFinished_ThrowsInvalidOperationException()
    {
        var run = CreateRun();
        run.Abort(Now);
        var member = run.OwnerMemberStates[0];
        var command = MakeCommand(member.ParticipantId, 1);

        var act = () => run.SubmitCommand(member.ParticipantId, command, Now);

        act.Should().Throw<InvalidOperationException>();
    }

    // --- AllOwnerCommandsSubmitted ---

    [Fact]
    public void AllOwnerCommandsSubmitted_WhenNoCommandsSubmitted_ReturnsFalse()
    {
        var run = CreateRun();

        run.AllOwnerCommandsSubmitted().Should().BeFalse();
    }

    [Fact]
    public void AllOwnerCommandsSubmitted_WhenAllCommandsSubmitted_ReturnsTrue()
    {
        var run = CreateRun();
        foreach (var member in run.OwnerMemberStates)
        {
            run.SubmitCommand(member.ParticipantId, MakeCommand(member.ParticipantId, 1), Now);
        }

        run.AllOwnerCommandsSubmitted().Should().BeTrue();
    }

    [Fact]
    public void AllOwnerCommandsSubmitted_AfterSwitchToAutoActionForTimeout_ReturnsTrue()
    {
        var run = CreateRun();

        run.SwitchToAutoActionForTimeout(Deadline.AddSeconds(1));

        run.AllOwnerCommandsSubmitted().Should().BeTrue();
    }

    // --- SwitchToAutoActionForTimeout ---

    [Fact]
    public void SwitchToAutoActionForTimeout_WhenDeadlineNotPassed_DoesNotSwitch()
    {
        var run = CreateRun();

        run.SwitchToAutoActionForTimeout(Deadline.AddSeconds(-1));

        run.OwnerMemberStates.Should().OnlyContain(m => m.ActionMode == ActionMode.Manual);
    }

    [Fact]
    public void SwitchToAutoActionForTimeout_WhenDeadlinePassed_SwitchesUnsubmittedMembers()
    {
        var run = CreateRun();

        run.SwitchToAutoActionForTimeout(Deadline.AddSeconds(1));

        run.OwnerMemberStates.Should().OnlyContain(m => m.ActionMode == ActionMode.AutoAttackOnly);
    }

    // --- ApplyBattleResolution ---

    [Fact]
    public void ApplyBattleResolution_WhenOpponentAllDead_SetsOwnerWon()
    {
        var run = CreateRun();
        var opponentStates = run.OpponentMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 0, 0));
        var ownerStates = run.OwnerMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));

        run.ApplyBattleResolution(ownerStates, opponentStates, MakeLastTurnResults(), Now.AddSeconds(60));

        run.Status.Should().Be(PetBattleRunStatus.OwnerWon);
        run.WinnerPlayerId.Should().Be(OwnerId);
        run.IsFinished.Should().BeTrue();
    }

    [Fact]
    public void ApplyBattleResolution_WhenOwnerAllDead_SetsOpponentWon()
    {
        var run = CreateRun();
        var ownerStates = run.OwnerMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 0, 0));
        var opponentStates = run.OpponentMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));

        run.ApplyBattleResolution(ownerStates, opponentStates, MakeLastTurnResults(), Now.AddSeconds(60));

        run.Status.Should().Be(PetBattleRunStatus.OpponentWon);
        run.WinnerPlayerId.Should().Be(OpponentId);
    }

    [Fact]
    public void ApplyBattleResolution_WhenBothAllDead_SetsOpponentWon()
    {
        var run = CreateRun();
        var ownerStates = run.OwnerMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 0, 0));
        var opponentStates = run.OpponentMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 0, 0));

        run.ApplyBattleResolution(ownerStates, opponentStates, MakeLastTurnResults(), Now.AddSeconds(60));

        run.Status.Should().Be(PetBattleRunStatus.OpponentWon);
    }

    [Fact]
    public void ApplyBattleResolution_WhenBattleContinues_AdvancesTurnAndRemainInProgress()
    {
        var run = CreateRun();
        var ownerStates = run.OwnerMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));
        var opponentStates = run.OpponentMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));

        run.ApplyBattleResolution(ownerStates, opponentStates, MakeLastTurnResults(), Now.AddSeconds(60));

        run.Status.Should().Be(PetBattleRunStatus.InProgress);
        run.TurnState.CurrentTurnNo.Should().Be(2);
        run.IsFinished.Should().BeFalse();
    }

    [Fact]
    public void ApplyBattleResolution_WhenBattleContinues_RestoressOwnerMembersToManual()
    {
        var run = CreateRun();
        run.SwitchToAutoActionForTimeout(Deadline.AddSeconds(1));
        var ownerStates = run.OwnerMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));
        var opponentStates = run.OpponentMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));

        run.ApplyBattleResolution(ownerStates, opponentStates, MakeLastTurnResults(), Now.AddSeconds(60));

        run.OwnerMemberStates.Should().OnlyContain(m => m.ActionMode == ActionMode.Manual);
    }

    [Fact]
    public void ApplyBattleResolution_WhenBattleContinues_IncrementsVersion()
    {
        var run = CreateRun();
        var initialVersion = run.Version;
        var ownerStates = run.OwnerMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));
        var opponentStates = run.OpponentMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));

        run.ApplyBattleResolution(ownerStates, opponentStates, MakeLastTurnResults(), Now.AddSeconds(60));

        run.Version.Should().Be(initialVersion + 1);
    }

    // --- Abort ---

    [Fact]
    public void Abort_SetsStatusToAborted()
    {
        var run = CreateRun();

        run.Abort(Now);

        run.Status.Should().Be(PetBattleRunStatus.Aborted);
        run.EndedAt.Should().Be(Now);
        run.IsFinished.Should().BeTrue();
    }

    [Fact]
    public void Abort_WhenAlreadyFinished_ThrowsInvalidOperationException()
    {
        var run = CreateRun();
        run.Abort(Now);

        var act = () => run.Abort(Now);

        act.Should().Throw<InvalidOperationException>();
    }

    // --- helpers ---

    private static PetBattleRun CreateRun(int ownerCount = 1, int opponentCount = 1)
    {
        var roomId = PetBattleRoomId.NewId();
        var ownerSnapshots = Enumerable.Range(0, ownerCount).Select(i =>
            MakeSnapshot(BattleRow.Front, i == 0 ? BattleColumn.Left : BattleColumn.Right)).ToArray();
        var opponentSnapshots = Enumerable.Range(0, opponentCount).Select(i =>
            MakeSnapshot(BattleRow.Front, i == 0 ? BattleColumn.Left : BattleColumn.Right)).ToArray();
        var ownerMembers = ownerSnapshots
            .Select(s => new PetBattlePartyMemberState(s.ParticipantId, 200, 100, false, 1, ActionMode.Manual))
            .ToArray();
        var opponentMembers = opponentSnapshots
            .Select(s => new PetBattlePartyMemberState(s.ParticipantId, 200, 100, false, 1, ActionMode.AutoAttackOnly))
            .ToArray();

        return new PetBattleRun(
            PetBattleRunId.NewId(),
            roomId,
            OwnerId,
            OpponentId,
            ownerSnapshots,
            opponentSnapshots,
            new PetBattleTurnState(1, Deadline),
            ownerMembers,
            opponentMembers,
            lastTurnResults: null,
            startedAt: Now);
    }

    private static PetBattlePartyMemberSnapshot MakeSnapshot(BattleRow row, BattleColumn column) =>
        new(
            PetBattleParticipantId.NewId(),
            enemyDefinitionId: 1,
            displayName: "テストペット",
            imagePath: null,
            new Status(200, 100, 50, 50, 50, 50, 50),
            moveIds: [1],
            row,
            column);

    private static PetBattleSubmittedCommand MakeCommand(PetBattleParticipantId participantId, int turnNo) =>
        new(participantId, turnNo, ActionKind.NormalAttack, DateTimeOffset.UtcNow);

    private static PetBattleLastTurnResults MakeLastTurnResults() =>
        new(1, Now, []);
}
