using server.domain.player;
using server.domain.quest.enums;
using server.domain.battle;

namespace server.domain.quest;

public class QuestRun(
    QuestRunId id,
    QuestRoomId roomId,
    QuestStageId stageId,
    IEnumerable<QuestRunPartyMemberSnapshot> partySnapshots,
    QuestFloorState floorState,
    QuestBattleState battleState,
    QuestTurnState turnState,
    QuestTrapCollection traps,
    QuestRewardAccumulator rewards,
    QuestLastTurnResults? lastTurnResults,
    IEnumerable<QuestChatMessage>? chatMessages,
    DateTimeOffset startedAt,
    QuestRunStatus status = QuestRunStatus.InProgress,
    DateTimeOffset? endedAt = null,
    int version = 1)
{
    private readonly List<QuestChatMessage> chatMessages = chatMessages?.ToList() ?? [];
    private readonly List<QuestChatMessage> pendingChatMessages = [];
    private readonly QuestRunPartyMemberSnapshot[] partySnapshots = partySnapshots?.ToArray()
        ?? throw new ArgumentNullException(nameof(partySnapshots));

    public QuestRunId Id { get; } = id;
    public QuestRoomId RoomId { get; } = roomId;
    public int Version { get; private set; } = version;
    public int PersistedVersion { get; private set; } = version;
    public QuestStageId StageId { get; } = stageId;
    public IReadOnlyList<QuestRunPartyMemberSnapshot> PartySnapshots => partySnapshots;
    public QuestRunStatus Status { get; private set; } = status;
    public QuestFloorState FloorState { get; } = floorState ?? throw new ArgumentNullException(nameof(floorState));
    public QuestBattleState BattleState { get; } = battleState ?? throw new ArgumentNullException(nameof(battleState));
    public QuestTurnState TurnState { get; } = turnState ?? throw new ArgumentNullException(nameof(turnState));
    public QuestTrapCollection Traps { get; } = traps ?? throw new ArgumentNullException(nameof(traps));
    public QuestRewardAccumulator Rewards { get; } = rewards ?? throw new ArgumentNullException(nameof(rewards));
    public QuestLastTurnResults? LastTurnResults { get; private set; } = lastTurnResults;

    // 直近ターン結果が再計算され、永続化が必要かどうか。軽量ロード（last_turn_results_json 未取得）でも
    // ダーティでない限り保存時に列を上書きしないことで、表示用ログの消失を防ぐ。
    public bool LastTurnResultsDirty { get; private set; }
    public IReadOnlyList<QuestChatMessage> ChatMessages => chatMessages;
    public IReadOnlyList<QuestChatMessage> PendingChatMessages => pendingChatMessages;
    public DateTimeOffset StartedAt { get; } = startedAt;
    public DateTimeOffset? EndedAt { get; private set; } = endedAt;

    public void InitializeFirstFloor()
    {
        EnsureInProgress();
        if (FloorState.CurrentFloorNo != 1)
        {
            FloorState.AdvanceTo(1, FloorState.IsBossFloor, FloorState.CurrentPlacements);
        }
    }

    public void SubmitCommand(QuestParticipantId participantId, QuestSubmittedCommand command, DateTimeOffset now)
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

        var partyMember = BattleState.FindPartyMember(participantId);
        if (!partyMember.CanAcceptManualCommand(TurnState.CurrentTurnNo))
        {
            throw new InvalidOperationException("手動入力できない行動モードです。");
        }

        if (command.ActionKind == ActionKind.Capture && command.SelectedTargetPosition is null)
        {
            throw new ArgumentException("捕獲には対象の敵の指定が必要です。", nameof(command));
        }

        if (command.ActionKind == ActionKind.SummonPet)
        {
            var snapshot = partySnapshots.FirstOrDefault(x => x.ParticipantId == participantId)
                ?? throw new KeyNotFoundException("参加者スナップショットが見つかりません。");
            if (snapshot.Pet is null)
            {
                throw new InvalidOperationException("スタンバイのペットがいないため呼出できません。");
            }

            if (!partyMember.HasRemainingPetSummons)
            {
                throw new InvalidOperationException("このクエストでの呼出回数の上限に達しています。");
            }
        }

        TurnState.Submit(command);
        Version++;
    }

    public void SwitchToAutoActionForTimeout(DateTimeOffset now)
    {
        EnsureInProgress();
        if (now <= TurnState.ActionDeadlineAt)
        {
            return;
        }

        var submittedIds = TurnState.PendingCommands.Select(x => x.ParticipantId).ToHashSet();
        foreach (var partyMember in BattleState.PartyMembers.Where(x => !x.IsDead && x.ActionMode == ActionMode.Manual))
        {
            if (!submittedIds.Contains(partyMember.ParticipantId))
            {
                partyMember.SwitchToAutoAttackOnly();
            }
        }

        Version++;
    }

    public void RequestManualControl(QuestParticipantId participantId)
    {
        EnsureInProgress();
        BattleState.FindPartyMember(participantId).RequestManualControl();
        Version++;
    }

    public void ApproveManualControl(QuestParticipantId participantId, PlayerId ownerId)
    {
        EnsureInProgress();
        _ = ownerId;
        var partyMember = BattleState.FindPartyMember(participantId);
        if (!partyMember.IsManualControlRequested)
        {
            throw new InvalidOperationException("手動復帰申請がないため承認できません。");
        }

        partyMember.SwitchToManual();
        Version++;
    }

    public void AdvanceFloor(bool isBossFloor = false, IEnumerable<QuestEnemyPlacement>? nextPlacements = null)
    {
        EnsureInProgress();
        if (!BattleState.AreAllEnemiesDefeated())
        {
            throw new InvalidOperationException("敵が残っているため階層を進めません。");
        }

        FloorState.AdvanceTo(FloorState.CurrentFloorNo + 1, isBossFloor, nextPlacements ?? []);
        Version++;
    }

    public void StartNextFloor(
        IEnumerable<QuestEnemyState> nextEnemies,
        bool isBossFloor,
        IEnumerable<QuestEnemyPlacement> nextPlacements,
        DateTimeOffset nextDeadlineAt)
    {
        EnsureInProgress();
        ArgumentNullException.ThrowIfNull(nextEnemies);
        ArgumentNullException.ThrowIfNull(nextPlacements);

        if (!BattleState.AreAllEnemiesDefeated())
        {
            throw new InvalidOperationException("敵が残っているため次階層へ進めません。");
        }

        AdvanceFloor(isBossFloor, nextPlacements);
        BattleState.ReplaceEnemies(nextEnemies);
        ApplyTrapsOnFloorStart();
        TurnState.Advance(nextDeadlineAt);
        Version++;
    }

    public void ApplyTrapsOnFloorStart()
    {
        EnsureInProgress();
        Traps.RemoveExpired(FloorState.CurrentFloorNo);
        Version++;
    }

    public QuestRunResolutionSummary ResolveTurn(DateTimeOffset nextDeadlineAt, bool escapeEndsAsSuccess = false)
    {
        EnsureInProgress();

        var escapeRequested = TurnState.PendingCommands.Any(x => x.ActionKind == ActionKind.Escape);
        foreach (var leaveCommand in TurnState.PendingCommands.Where(x => x.ActionKind == ActionKind.LeaveQuest))
        {
            BattleState.FindPartyMember(leaveCommand.ParticipantId).LeaveQuest();
        }

        var isFloorCleared = BattleState.AreAllEnemiesDefeated();
        var isQuestCompleted = false;
        var isQuestFailed = false;

        if (escapeRequested)
        {
            // エンドレスのチェックポイント終了は生存撤退でも「成功」扱いにする。
            if (escapeEndsAsSuccess)
            {
                MarkSucceeded();
                isQuestCompleted = true;
            }
            else
            {
                MarkFailed();
                isQuestFailed = true;
            }
        }
        else if (!BattleState.HasContinuablePartyMember())
        {
            MarkFailed();
            isQuestFailed = true;
        }

        TurnState.MarkResolved();

        if (Status == QuestRunStatus.InProgress && !isFloorCleared)
        {
            TurnState.Advance(nextDeadlineAt);
        }

        return new QuestRunResolutionSummary(
            turn: TurnState.LastResolvedTurnNo ?? TurnState.CurrentTurnNo,
            isFloorCleared: isFloorCleared,
            isQuestCompleted: isQuestCompleted,
            isQuestFailed: isQuestFailed);

    }

    public QuestRunResolutionSummary ApplyBattleResolution(
        BattleTurnResolution resolution,
        IReadOnlyDictionary<BattleActorId, QuestParticipantId> partyActorMap,
        IReadOnlyDictionary<BattleActorId, QuestEnemyInstanceId> enemyActorMap,
        int finalFloorNo,
        DateTimeOffset nextDeadlineAt,
        bool escapeEndsAsSuccess = false)
    {
        EnsureInProgress();
        BattleState.ApplyResolution(resolution, partyActorMap, enemyActorMap, TurnState.CurrentTurnNo);

        var summary = ResolveTurn(nextDeadlineAt, escapeEndsAsSuccess);
        Version++;
        if (Status == QuestRunStatus.InProgress &&
            summary.IsFloorCleared &&
            FloorState.CurrentFloorNo >= finalFloorNo)
        {
            MarkSucceeded();
            return new QuestRunResolutionSummary(
                turn: summary.Turn,
                isFloorCleared: true,
                isQuestCompleted: true,
                isQuestFailed: false);
        }

        return summary;
    }

    public void AddChatMessage(QuestChatMessage message)
    {
        EnsureInProgress();
        ArgumentNullException.ThrowIfNull(message);
        chatMessages.Add(message);
        pendingChatMessages.Add(message);
        Version++;
    }

    public void SetChatMessagesForPersistence(IEnumerable<QuestChatMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        chatMessages.Clear();
        chatMessages.AddRange(messages);
    }

    public void MarkChatMessagesPersisted()
    {
        pendingChatMessages.Clear();
    }

    public void SetLastTurnResults(QuestLastTurnResults? lastTurnResults)
    {
        LastTurnResults = lastTurnResults;
        LastTurnResultsDirty = true;
        Version++;
    }

    public void MarkLastTurnResultsPersisted()
    {
        LastTurnResultsDirty = false;
    }

    public void EscapeByOwner()
    {
        MarkFailed();
    }

    public void MarkSucceeded()
    {
        EnsureInProgress();
        Status = QuestRunStatus.Succeeded;
        EndedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public void MarkFailed()
    {
        EnsureInProgress();
        Status = QuestRunStatus.Failed;
        EndedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public void Abort(string reason)
    {
        EnsureInProgress();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("中断理由は必須です。", nameof(reason));
        }

        Status = QuestRunStatus.Aborted;
        EndedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public void SyncVersion(int version)
    {
        Version = version;
        PersistedVersion = version;
    }

    private void EnsureInProgress()
    {
        if (Status != QuestRunStatus.InProgress)
        {
            throw new InvalidOperationException("進行中のクエストでのみ実行できます。");
        }
    }
}
