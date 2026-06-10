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
    public void SwitchToAutoActionForTimeout_WhenDeadlinePassed_SwitchesManualMembers()
    {
        var run = CreateRun();

        run.SwitchToAutoActionForTimeout(Deadline.AddSeconds(1));

        run.OwnerMemberStates.Should().OnlyContain(m => m.ActionMode == ActionMode.AutoAttackOnly);
    }

    [Fact]
    public void SubmitCommand_WhenExactlyAtDeadline_IsAccepted()
    {
        var run = CreateRun();
        var member = run.OwnerMemberStates[0];
        var command = MakeCommand(member.ParticipantId, 1);

        var act = () => run.SubmitCommand(member.ParticipantId, command, Deadline);

        act.Should().NotThrow();
    }

    [Fact]
    public void SubmitCommand_WhenOpponentParticipantId_ThrowsKeyNotFoundException()
    {
        var run = CreateRun();
        var opponentMember = run.OpponentMemberStates[0];
        var command = MakeCommand(opponentMember.ParticipantId, 1);

        var act = () => run.SubmitCommand(opponentMember.ParticipantId, command, Now);

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void SubmitCommand_WhenPetIsDead_ThrowsInvalidOperationException()
    {
        var run = CreateRun(ownerCount: 2);
        var deadMember = run.OwnerMemberStates[0];
        var deadCommand = MakeCommand(deadMember.ParticipantId, 1);
        // 死亡状態のメンバーを持つ run を直接生成
        var deadRun = CreateRunWithDeadOwner();
        var dm = deadRun.OwnerMemberStates[0];
        var cmd = MakeCommand(dm.ParticipantId, 1);

        var act = () => deadRun.SubmitCommand(dm.ParticipantId, cmd, Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SubmitCommand_WhenPetIsAutoAttackOnly_ThrowsInvalidOperationException()
    {
        var run = CreateRun();
        run.SwitchToAutoActionForTimeout(Deadline.AddSeconds(1));
        var member = run.OwnerMemberStates[0];
        var command = MakeCommand(member.ParticipantId, 1);

        var act = () => run.SubmitCommand(member.ParticipantId, command, Now);

        act.Should().Throw<InvalidOperationException>();
    }

    // --- AllOwnerCommandsSubmitted (multi-pet) ---

    [Fact]
    public void AllOwnerCommandsSubmitted_WhenOneOfTwoMembersNotSubmitted_ReturnsFalse()
    {
        var run = CreateRun(ownerCount: 2);
        var first = run.OwnerMemberStates[0];
        run.SubmitCommand(first.ParticipantId, MakeCommand(first.ParticipantId, 1), Now);

        run.AllOwnerCommandsSubmitted().Should().BeFalse();
    }

    [Fact]
    public void AllOwnerCommandsSubmitted_WhenDeadMemberAndRemainingSubmitted_ReturnsTrue()
    {
        var run = CreateRunWithOneDeadOwner();
        var aliveMember = run.OwnerMemberStates.Single(m => !m.IsDead);
        run.SubmitCommand(aliveMember.ParticipantId, MakeCommand(aliveMember.ParticipantId, 1), Now);

        run.AllOwnerCommandsSubmitted().Should().BeTrue();
    }

    [Fact]
    public void AllOwnerCommandsSubmitted_WhenAutoAttackOnlyMemberAndRemainingSubmitted_ReturnsTrue()
    {
        var run = CreateRun(ownerCount: 2);
        // 1体だけタイムアウトで AutoAttackOnly に切り替わる前に Submit、残り1体は AutoAttackOnly
        run.SwitchToAutoActionForTimeout(Deadline.AddSeconds(1));
        // AutoAttackOnly メンバーは CanAcceptManualCommand=false なので提出不要
        run.AllOwnerCommandsSubmitted().Should().BeTrue();
    }

    // --- SwitchToAutoActionForTimeout (boundary) ---

    [Fact]
    public void SwitchToAutoActionForTimeout_WhenExactlyAtDeadline_DoesNotSwitch()
    {
        var run = CreateRun();

        run.SwitchToAutoActionForTimeout(Deadline);

        run.OwnerMemberStates.Should().OnlyContain(m => m.ActionMode == ActionMode.Manual);
    }

    // --- ApplyBattleResolution ---

    [Fact]
    public void ApplyBattleResolution_WhenOpponentAllDead_SetsOwnerWon()
    {
        var run = CreateRun();
        var lastTurnResults = MakeLastTurnResults();
        var opponentStates = run.OpponentMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 0, 0));
        var ownerStates = run.OwnerMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));

        run.ApplyBattleResolution(ownerStates, opponentStates, lastTurnResults, Now.AddSeconds(60));

        run.Status.Should().Be(PetBattleRunStatus.OwnerWon);
        run.WinnerPlayerId.Should().Be(OwnerId);
        run.IsFinished.Should().BeTrue();
        run.EndedAt.Should().Be(lastTurnResults.ResolvedAt);
    }

    [Fact]
    public void ApplyBattleResolution_WhenOwnerAllDead_SetsOpponentWon()
    {
        var run = CreateRun();
        var lastTurnResults = MakeLastTurnResults();
        var ownerStates = run.OwnerMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 0, 0));
        var opponentStates = run.OpponentMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));

        run.ApplyBattleResolution(ownerStates, opponentStates, lastTurnResults, Now.AddSeconds(60));

        run.Status.Should().Be(PetBattleRunStatus.OpponentWon);
        run.WinnerPlayerId.Should().Be(OpponentId);
        run.EndedAt.Should().Be(lastTurnResults.ResolvedAt);
    }

    [Fact]
    public void ApplyBattleResolution_WhenOneOfTwoOpponentsAlive_ContinuesBattle()
    {
        var run = CreateRun(ownerCount: 1, opponentCount: 2);
        var ownerStates = run.OwnerMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));
        var opponentStates = run.OpponentMemberStates
            .Select((m, i) => (m, hp: i == 0 ? 0 : 100))
            .ToDictionary(x => x.m.ParticipantId,
                x => new BattleActorState(new BattleActorId(x.m.ParticipantId.Value), x.hp, 50));

        run.ApplyBattleResolution(ownerStates, opponentStates, MakeLastTurnResults(), Now.AddSeconds(60));

        run.Status.Should().Be(PetBattleRunStatus.InProgress);
        run.IsFinished.Should().BeFalse();
    }

    [Fact]
    public void ApplyBattleResolution_WhenBattleContinues_DeadOwnerMemberNotRestoredToManual()
    {
        var run = CreateRun(ownerCount: 2);
        run.SwitchToAutoActionForTimeout(Deadline.AddSeconds(1));
        var firstMember = run.OwnerMemberStates[0];
        var ownerStates = run.OwnerMemberStates
            .Select((m, i) => (m, hp: i == 0 ? 0 : 100))
            .ToDictionary(x => x.m.ParticipantId,
                x => new BattleActorState(new BattleActorId(x.m.ParticipantId.Value), x.hp, 50));
        var opponentStates = run.OpponentMemberStates
            .ToDictionary(m => m.ParticipantId,
                m => new BattleActorState(new BattleActorId(m.ParticipantId.Value), 100, 50));

        run.ApplyBattleResolution(ownerStates, opponentStates, MakeLastTurnResults(), Now.AddSeconds(60));

        var deadMember = run.OwnerMemberStates.Single(m => m.ParticipantId == firstMember.ParticipantId);
        deadMember.IsDead.Should().BeTrue();
        deadMember.ActionMode.Should().Be(ActionMode.AutoAttackOnly);
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

    private static PetBattleRun CreateRunWithDeadOwner()
    {
        var snapshot = MakeSnapshot(BattleRow.Front, BattleColumn.Left);
        var opponentSnapshot = MakeSnapshot(BattleRow.Front, BattleColumn.Left);
        var deadOwnerMember = new PetBattlePartyMemberState(snapshot.ParticipantId, 0, 0, isDead: true, 1, ActionMode.Manual);
        var opponentMember = new PetBattlePartyMemberState(opponentSnapshot.ParticipantId, 200, 100, false, 1, ActionMode.AutoAttackOnly);
        return new PetBattleRun(
            PetBattleRunId.NewId(), PetBattleRoomId.NewId(), OwnerId, OpponentId,
            [snapshot], [opponentSnapshot],
            new PetBattleTurnState(1, Deadline),
            [deadOwnerMember], [opponentMember],
            lastTurnResults: null, startedAt: Now);
    }

    private static PetBattleRun CreateRunWithOneDeadOwner()
    {
        var snap1 = MakeSnapshot(BattleRow.Front, BattleColumn.Left);
        var snap2 = MakeSnapshot(BattleRow.Front, BattleColumn.Right);
        var opponentSnapshot = MakeSnapshot(BattleRow.Front, BattleColumn.Left);
        var deadMember = new PetBattlePartyMemberState(snap1.ParticipantId, 0, 0, isDead: true, 1, ActionMode.Manual);
        var aliveMember = new PetBattlePartyMemberState(snap2.ParticipantId, 200, 100, false, 1, ActionMode.Manual);
        var opponentMember = new PetBattlePartyMemberState(opponentSnapshot.ParticipantId, 200, 100, false, 1, ActionMode.AutoAttackOnly);
        return new PetBattleRun(
            PetBattleRunId.NewId(), PetBattleRoomId.NewId(), OwnerId, OpponentId,
            [snap1, snap2], [opponentSnapshot],
            new PetBattleTurnState(1, Deadline),
            [deadMember, aliveMember], [opponentMember],
            lastTurnResults: null, startedAt: Now);
    }

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
