using server.domain.battle;
using server.domain.pet_battle.enums;
using server.domain.player;
using server.domain.quest.enums;

namespace server.domain.pet_battle;

public class PetBattleRun(
    PetBattleRunId id,
    PetBattleRoomId roomId,
    PlayerId ownerPlayerId,
    PlayerId opponentPlayerId,
    IEnumerable<PetBattlePartyMemberSnapshot> ownerSnapshots,
    IEnumerable<PetBattlePartyMemberSnapshot> opponentSnapshots,
    PetBattleTurnState turnState,
    IEnumerable<PetBattlePartyMemberState> ownerMemberStates,
    IEnumerable<PetBattlePartyMemberState> opponentMemberStates,
    PetBattleLastTurnResults? lastTurnResults,
    DateTimeOffset startedAt,
    PetBattleRunStatus status = PetBattleRunStatus.InProgress,
    DateTimeOffset? endedAt = null,
    PlayerId? winnerPlayerId = null,
    int version = 1)
{
    private readonly PetBattlePartyMemberSnapshot[] ownerSnapshots = ownerSnapshots?.ToArray()
        ?? throw new ArgumentNullException(nameof(ownerSnapshots));
    private readonly PetBattlePartyMemberSnapshot[] opponentSnapshots = opponentSnapshots?.ToArray()
        ?? throw new ArgumentNullException(nameof(opponentSnapshots));
    private readonly List<PetBattlePartyMemberState> ownerMemberStates = ownerMemberStates?.ToList()
        ?? throw new ArgumentNullException(nameof(ownerMemberStates));
    private readonly List<PetBattlePartyMemberState> opponentMemberStates = opponentMemberStates?.ToList()
        ?? throw new ArgumentNullException(nameof(opponentMemberStates));

    public PetBattleRunId Id { get; } = id;
    public PetBattleRoomId RoomId { get; } = roomId;
    public int Version { get; private set; } = version;
    public int PersistedVersion { get; private set; } = version;
    public PlayerId OwnerPlayerId { get; } = ownerPlayerId;
    public PlayerId OpponentPlayerId { get; } = opponentPlayerId;
    public IReadOnlyList<PetBattlePartyMemberSnapshot> OwnerSnapshots => ownerSnapshots;
    public IReadOnlyList<PetBattlePartyMemberSnapshot> OpponentSnapshots => opponentSnapshots;
    public PetBattleTurnState TurnState { get; } = turnState ?? throw new ArgumentNullException(nameof(turnState));
    public IReadOnlyList<PetBattlePartyMemberState> OwnerMemberStates => ownerMemberStates;
    public IReadOnlyList<PetBattlePartyMemberState> OpponentMemberStates => opponentMemberStates;
    public PetBattleLastTurnResults? LastTurnResults { get; private set; } = lastTurnResults;
    public DateTimeOffset StartedAt { get; } = startedAt;
    public PetBattleRunStatus Status { get; private set; } = status;
    public DateTimeOffset? EndedAt { get; private set; } = endedAt;
    public PlayerId? WinnerPlayerId { get; private set; } = winnerPlayerId;

    public void SubmitCommand(PetBattleParticipantId participantId, PetBattleSubmittedCommand command, DateTimeOffset now)
    {
        EnsureInProgress();
        ArgumentNullException.ThrowIfNull(command);

        if (command.ParticipantId != participantId)
        {
            throw new InvalidOperationException("参加者と行動の主体が一致していません。");
        }

        if (now > TurnState.ActionDeadlineAt)
        {
            throw new InvalidOperationException("行動受付期限を過ぎています。");
        }

        var member = FindOwnerMember(participantId);
        if (!member.CanAcceptManualCommand(TurnState.CurrentTurnNo))
        {
            throw new InvalidOperationException("手動入力できない状態のペットです。");
        }

        TurnState.Submit(command);
        Version++;
    }

    public bool AllOwnerCommandsSubmitted()
    {
        var active = ownerMemberStates
            .Where(m => m.CanAcceptManualCommand(TurnState.CurrentTurnNo))
            .ToArray();
        return active.All(m => TurnState.PendingCommands.Any(c => c.ParticipantId == m.ParticipantId));
    }

    public void SwitchToAutoActionForTimeout(DateTimeOffset now)
    {
        EnsureInProgress();
        if (now <= TurnState.ActionDeadlineAt)
        {
            return;
        }

        foreach (var member in ownerMemberStates.Where(m => m.CanAcceptManualCommand(TurnState.CurrentTurnNo)))
        {
            member.SwitchToAutoAttackOnly();
        }
    }

    public void ApplyBattleResolution(
        IReadOnlyDictionary<PetBattleParticipantId, BattleActorState> ownerActorStates,
        IReadOnlyDictionary<PetBattleParticipantId, BattleActorState> opponentActorStates,
        PetBattleLastTurnResults lastTurnResults,
        DateTimeOffset nextDeadlineAt)
    {
        EnsureInProgress();

        foreach (var member in ownerMemberStates)
        {
            if (ownerActorStates.TryGetValue(member.ParticipantId, out var state))
            {
                member.ApplyBattleState(state, TurnState.CurrentTurnNo);
            }
        }

        foreach (var member in opponentMemberStates)
        {
            if (opponentActorStates.TryGetValue(member.ParticipantId, out var state))
            {
                member.ApplyBattleState(state, TurnState.CurrentTurnNo);
            }
        }

        // タイムアウトで AutoAttackOnly になった手動ペットを次ターン用に Manual に戻す
        foreach (var member in ownerMemberStates.Where(m => !m.IsDead))
        {
            member.RestoreManualMode();
        }

        LastTurnResults = lastTurnResults;
        TurnState.Advance(nextDeadlineAt);
        Version++;

        var ownerAllDead = ownerMemberStates.All(m => m.IsDead);
        var opponentAllDead = opponentMemberStates.All(m => m.IsDead);

        if (ownerAllDead || opponentAllDead)
        {
            if (!ownerAllDead)
            {
                Status = PetBattleRunStatus.OwnerWon;
                WinnerPlayerId = OwnerPlayerId;
            }
            else if (!opponentAllDead)
            {
                Status = PetBattleRunStatus.OpponentWon;
                WinnerPlayerId = OpponentPlayerId;
            }
            else
            {
                // 引き分けはオーナー負けとする
                Status = PetBattleRunStatus.OpponentWon;
                WinnerPlayerId = OpponentPlayerId;
            }

            EndedAt = lastTurnResults.ResolvedAt;
        }
    }

    public void Abort(DateTimeOffset now)
    {
        EnsureInProgress();
        Status = PetBattleRunStatus.Aborted;
        EndedAt = now;
        Version++;
    }

    public bool IsFinished => Status != PetBattleRunStatus.InProgress;

    public void SyncVersion(int version)
    {
        Version = version;
        PersistedVersion = version;
    }

    private PetBattlePartyMemberState FindOwnerMember(PetBattleParticipantId participantId) =>
        ownerMemberStates.FirstOrDefault(m => m.ParticipantId == participantId)
            ?? throw new KeyNotFoundException($"オーナーのペットが見つかりません。 participantId={participantId.Value}");

    private void EnsureInProgress()
    {
        if (Status != PetBattleRunStatus.InProgress)
        {
            throw new InvalidOperationException("対戦は進行中ではありません。");
        }
    }
}
