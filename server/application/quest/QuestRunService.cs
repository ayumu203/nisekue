using server.application.battle;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestRunService(
    IQuestRunRepository questRunRepository,
    IQuestRoomRepository questRoomRepository,
    IQuestStageRepository questStageRepository,
    IQuestEnemyDefinitionRepository questEnemyDefinitionRepository,
    IMoveRepository moveRepository,
    IPlayerRepository playerRepository,
    IJobProfileRepository jobProfileRepository,
    IJobMoveLearningRuleRepository jobMoveLearningRuleRepository,
    BattleService battleService,
    QuestBattleFactory questBattleFactory)
{
    private static readonly TimeSpan TurnDeadline = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan QuestCooldown = TimeSpan.FromMinutes(1);

    public async Task<QuestRun> GetDetailAsync(QuestRunId runId)
    {
        return await questRunRepository.GetAsync(runId)
            ?? throw new KeyNotFoundException("クエスト進行情報が見つかりません。");
    }

    public async Task<QuestCommandSubmissionResult> SubmitCommandAsync(QuestRunId runId, QuestParticipantId participantId, QuestSubmittedCommand command)
    {
        var run = await GetDetailAsync(runId);

        run.SubmitCommand(participantId, command, DateTimeOffset.UtcNow);
        var resolved = await TryResolveIfReadyAsync(run);
        await questRunRepository.SaveAsync(run);
        return new QuestCommandSubmissionResult(run, resolved);
    }

    public async Task<QuestRun> RequestManualControlAsync(QuestRunId runId, QuestParticipantId participantId)
    {
        var run = await GetDetailAsync(runId);
        run.RequestManualControl(participantId);
        await questRunRepository.SaveAsync(run);
        return run;
    }

    public async Task<QuestRun> ApproveManualControlAsync(QuestRunId runId, QuestParticipantId participantId, PlayerId ownerId)
    {
        var run = await GetDetailAsync(runId);
        run.ApproveManualControl(participantId, ownerId);
        await questRunRepository.SaveAsync(run);
        return run;
    }

    public async Task<QuestRun> EscapeAsync(QuestRunId runId, PlayerId ownerPlayerId)
    {
        var run = await GetDetailAsync(runId);
        var room = await questRoomRepository.GetAsync(run.RoomId)
            ?? throw new KeyNotFoundException($"ルームが見つかりません。 roomId={run.RoomId.Value}");
        if (room.OwnerId != ownerPlayerId)
        {
            throw new InvalidOperationException("ルームのオーナーのみ撤退を実行できます。");
        }

        run.EscapeByOwner();
        await ApplyQuestCompletionEffectsAsync(run);
        await questRunRepository.SaveAsync(run);
        return run;
    }

    public async Task<IReadOnlyList<QuestRun>> ProcessExpiredRunsAsync(DateTimeOffset now)
    {
        var expiredRuns = await questRunRepository.ListExpiredAsync(now);
        if (expiredRuns.Count == 0)
        {
            return [];
        }

        var updatedRuns = new List<QuestRun>();
        foreach (var run in expiredRuns)
        {
            run.SwitchToAutoActionForTimeout(now);
            var changed = await TryResolveIfReadyAsync(run);
            if (changed)
            {
                await questRunRepository.SaveAsync(run);
                updatedRuns.Add(run);
            }
        }

        return updatedRuns;
    }

    public async Task<QuestRun> AddChatMessageAsync(QuestRunId runId, QuestChatMessage message)
    {
        var run = await GetDetailAsync(runId);
        run.AddChatMessage(message);
        await questRunRepository.SaveAsync(run);
        return run;
    }

    private async Task<bool> TryResolveIfReadyAsync(QuestRun run)
    {
        if (run.Status != QuestRunStatus.InProgress)
        {
            return false;
        }

        var waitingParticipantIds = GetWaitingParticipantIds(run);
        if (waitingParticipantIds.Count > 0)
        {
            return false;
        }

        var stage = await questStageRepository.GetAsync(run.StageId)
            ?? throw new KeyNotFoundException("ステージ定義が見つかりません。");
        var enemyDefinitions = (await questEnemyDefinitionRepository.GetAllAsync())
            .ToDictionary(x => x.Id);

        var beforeParty = run.BattleState.PartyMembers.ToDictionary(
            x => x.ParticipantId,
            x => new ActorStateSnapshot(x.CurrentHp, x.CurrentMp, x.IsDead));
        var beforeEnemy = run.BattleState.Enemies.ToDictionary(
            x => x.Id,
            x => new ActorStateSnapshot(x.CurrentHp, x.CurrentMp, x.IsDead));
        var previousFloorNo = run.FloorState.CurrentFloorNo;
        var previousStatus = run.Status.ToString();

        var fieldContext = questBattleFactory.CreateBattleFieldContext(run);
        var actors = questBattleFactory.CreateActorInputs(run);
        var (actions, moves) = await questBattleFactory.CreateTurnInputsAsync(run, moveRepository);
        var resolution = battleService.ResolveTurn(new BattleTurnRequest(actors, actions, moves, fieldContext));

        var finalFloorNo = stage.Floors.Max(x => x.FloorNo);
        var currentFloor = stage.Floors.FirstOrDefault(x => x.FloorNo == previousFloorNo)
            ?? throw new InvalidOperationException($"現在階層の定義が見つかりません。 floorNo={previousFloorNo}");
        var partyActorMap = questBattleFactory.CreatePartyActorMap(run);
        var enemyActorMap = questBattleFactory.CreateEnemyActorMap(run);

        var summary = run.ApplyBattleResolution(
            resolution,
            partyActorMap,
            enemyActorMap,
            finalFloorNo,
            DateTimeOffset.UtcNow.Add(TurnDeadline));

        QuestFloorDefinition? nextFloor = null;
        QuestEnemyState[]? nextEnemyStates = null;
        if (run.Status == QuestRunStatus.InProgress && summary.IsFloorCleared && !summary.IsQuestCompleted)
        {
            var nextFloorNo = previousFloorNo + 1;
            nextFloor = stage.Floors.FirstOrDefault(x => x.FloorNo == nextFloorNo)
                ?? throw new InvalidOperationException($"次階層が見つかりません。 floorNo={nextFloorNo}");

            nextEnemyStates = await CreateEnemyStatesAsync(nextFloor);
        }

        run.SetLastTurnResults(BuildLastTurnResults(
            run,
            resolution,
            moves,
            enemyDefinitions,
            partyActorMap,
            enemyActorMap,
            beforeParty,
            beforeEnemy,
            previousFloorNo,
            previousStatus,
            nextFloor?.FloorNo,
            nextFloor is not null && nextFloor.FloorType == FloorType.Boss,
            summary));

        if (summary.IsFloorCleared)
        {
            run.Rewards.AddExp(CalculateFloorExp(currentFloor, enemyDefinitions));
        }

        if (nextFloor is not null && nextEnemyStates is not null)
        {
            run.StartNextFloor(
                nextEnemyStates,
                nextFloor.FloorType == FloorType.Boss,
                nextFloor.Placements,
                DateTimeOffset.UtcNow.Add(TurnDeadline));
        }

        if (run.Status != QuestRunStatus.InProgress)
        {
            await ApplyQuestCompletionEffectsAsync(run);
        }

        return true;
    }

    private static int CalculateFloorExp(
        QuestFloorDefinition floor,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions)
    {
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(enemyDefinitions);

        var baseExp = floor.Placements.Sum(placement =>
            enemyDefinitions.TryGetValue(placement.EnemyDefinitionId, out var definition)
                ? definition.Level
                : 0);

        return (int)Math.Floor(baseExp * floor.RewardRule.ExpRate);
    }

    private async Task ApplyQuestCompletionEffectsAsync(QuestRun run)
    {
        var room = await questRoomRepository.GetAsync(run.RoomId)
            ?? throw new KeyNotFoundException($"ルームが見つかりません。 roomId={run.RoomId.Value}");

        var rewardedPlayerIds = room.Participants
            .Where(participant => participant.PlayerId is not null)
            .Where(participant =>
            {
                var state = run.BattleState.FindPartyMember(participant.Id);
                return !state.HasLeftQuest;
            })
            .Select(participant => participant.PlayerId!.Value)
            .Distinct()
            .ToArray();

        foreach (var playerId in rewardedPlayerIds)
        {
            var player = await playerRepository.GetPlayerAsync(playerId)
                ?? throw new KeyNotFoundException($"プレイヤーが見つかりません。 playerId={playerId.Value}");

            if (run.Rewards.Exp > 0)
            {
                player.GainExp(run.Rewards.Exp);
                var jobProfile = jobProfileRepository.GetByJob(player.Job);
                var learningRule = jobMoveLearningRuleRepository.GetByJob(player.Job);
                player.LevelUp(jobProfile, learningRule);
            }

            player.SetQuestCooldownUntil((run.EndedAt ?? DateTimeOffset.UtcNow).Add(QuestCooldown));
            await playerRepository.SaveAsync(player);
        }
    }

    private async Task<QuestEnemyState[]> CreateEnemyStatesAsync(QuestFloorDefinition floor)
    {
        ArgumentNullException.ThrowIfNull(floor);

        var enemyStates = new List<QuestEnemyState>(floor.Placements.Count);
        foreach (var placement in floor.Placements)
        {
            var enemyDefinition = await questEnemyDefinitionRepository.GetAsync(placement.EnemyDefinitionId)
                ?? throw new KeyNotFoundException($"敵定義が見つかりません。 enemyDefinitionId={placement.EnemyDefinitionId}");

            enemyStates.Add(new QuestEnemyState(
                QuestEnemyInstanceId.New(),
                enemyDefinition.Id,
                placement.Position,
                enemyDefinition.Status.MaxHp,
                enemyDefinition.Status.MaxMp,
                isDead: false));
        }

        return enemyStates.ToArray();
    }

    private static IReadOnlyList<QuestParticipantId> GetWaitingParticipantIds(QuestRun run)
    {
        var submittedIds = run.TurnState.PendingCommands
            .Select(x => x.ParticipantId)
            .ToHashSet();

        return run.BattleState.PartyMembers
            .Where(x => x.CanAcceptManualCommand(run.TurnState.CurrentTurnNo))
            .Select(x => x.ParticipantId)
            .Where(x => !submittedIds.Contains(x))
            .ToArray();
    }

    private static QuestLastTurnResults BuildLastTurnResults(
        QuestRun run,
        BattleTurnResolution resolution,
        IReadOnlyList<Move> moves,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions,
        IReadOnlyDictionary<BattleActorId, QuestParticipantId> partyActorMap,
        IReadOnlyDictionary<BattleActorId, QuestEnemyInstanceId> enemyActorMap,
        IReadOnlyDictionary<QuestParticipantId, ActorStateSnapshot> beforeParty,
        IReadOnlyDictionary<QuestEnemyInstanceId, ActorStateSnapshot> beforeEnemy,
        int previousFloorNo,
        string previousStatus,
        int? nextFloorNo,
        bool nextFloorIsBoss,
        QuestRunResolutionSummary summary)
    {
        var moveById = moves.ToDictionary(x => x.Id.Id);
        var snapshotByParticipantId = run.PartySnapshots.ToDictionary(x => x.ParticipantId);
        var partyById = run.BattleState.PartyMembers.ToDictionary(x => x.ParticipantId);
        var enemyById = run.BattleState.Enemies.ToDictionary(x => x.Id);

        var actions = resolution.ActionResults.Select(actionResult =>
        {
            Guid? actorParticipantId = null;
            Guid? actorEnemyInstanceId = null;
            string actorDisplayName;

            if (partyActorMap.TryGetValue(actionResult.ActorId, out var participantId))
            {
                actorParticipantId = participantId.Value;
                actorDisplayName = snapshotByParticipantId[participantId].DisplayName;
            }
            else if (enemyActorMap.TryGetValue(actionResult.ActorId, out var enemyInstanceId))
            {
                actorEnemyInstanceId = enemyInstanceId.Value;
                var enemy = enemyById[enemyInstanceId];
                actorDisplayName = enemyDefinitions.TryGetValue(enemy.EnemyDefinitionId, out var enemyDefinition)
                    ? enemyDefinition.Name
                    : enemy.EnemyDefinitionId.ToString();
            }
            else
            {
                actorDisplayName = actionResult.ActorId.Value.ToString();
            }

            var targetSummaries = actionResult.TargetResults.Select(targetResult =>
            {
                Guid? targetParticipantId = null;
                Guid? targetEnemyInstanceId = null;
                string targetDisplayName;
                var hpChange = targetResult.HpChange;
                var mpChange = targetResult.MpChange;
                string[] appliedEffects = targetResult.AppliedAilment is null ? [] : [targetResult.AppliedAilment.Value.ToString()];
                var removedEffects = Array.Empty<string>();
                var isDeadAfterAction = targetResult.IsDefeated;

                if (partyActorMap.TryGetValue(targetResult.TargetActorId, out var partyTargetId))
                {
                    targetParticipantId = partyTargetId.Value;
                    targetDisplayName = snapshotByParticipantId[partyTargetId].DisplayName;
                }
                else if (enemyActorMap.TryGetValue(targetResult.TargetActorId, out var enemyTargetId))
                {
                    targetEnemyInstanceId = enemyTargetId.Value;
                    var enemy = enemyById[enemyTargetId];
                    targetDisplayName = enemyDefinitions.TryGetValue(enemy.EnemyDefinitionId, out var enemyDefinition)
                        ? enemyDefinition.Name
                        : enemy.EnemyDefinitionId.ToString();
                }
                else
                {
                    targetDisplayName = targetResult.TargetActorId.Value.ToString();
                }

                var resultType = targetResult.IsDefeated
                    ? "Defeated"
                    : targetResult.AppliedAilment is not null
                        ? "AilmentApplied"
                        : targetResult.Damage > 0 || hpChange != 0 || mpChange != 0
                            ? "Hit"
                            : "Miss";

                return new QuestResolvedTargetSummary(
                    targetParticipantId,
                    targetEnemyInstanceId,
                    targetDisplayName,
                    resultType,
                    hpChange,
                    mpChange,
                    appliedEffects,
                    removedEffects,
                    isDeadAfterAction);
            }).ToArray();

            return new QuestResolvedAction(
                actorParticipantId,
                actorEnemyInstanceId,
                actorDisplayName,
                MapActionKind(actionResult.ActionKind),
                actionResult.MoveId?.Id,
                actionResult.MoveId is null ? null : moveById.GetValueOrDefault(actionResult.MoveId.Id)?.Name,
                actionResult.Succeeded,
                targetSummaries,
                BuildActionLogs(
                    actorDisplayName,
                    MapActionKind(actionResult.ActionKind),
                    actionResult.MoveId is null ? null : moveById.GetValueOrDefault(actionResult.MoveId.Id)?.Name,
                    actionResult.Succeeded,
                    actionResult.FailureReason,
                    targetSummaries));
        }).ToArray();

        var floorTransition = summary.IsFloorCleared
            ? new QuestFloorTransition(
                previousFloorNo,
                nextFloorNo ?? run.FloorState.CurrentFloorNo,
                true,
                nextFloorNo.HasValue ? nextFloorIsBoss : run.FloorState.IsBossFloor)
            : null;

        var runTransition = new QuestRunTransition(previousStatus, run.Status.ToString(), run.Status != QuestRunStatus.InProgress);
        var turnChatMessages = run.ChatMessages
            .Where(message => message.TurnNo == summary.Turn)
            .ToArray();

        return new QuestLastTurnResults(
            summary.Turn,
            DateTimeOffset.UtcNow,
            turnChatMessages,
            actions,
            floorTransition,
            runTransition);
    }

    private static string MapActionKind(server.domain.battle.enums.BattleActionKind actionKind)
    {
        return actionKind switch
        {
            server.domain.battle.enums.BattleActionKind.NormalAttack => ActionKind.NormalAttack.ToString(),
            server.domain.battle.enums.BattleActionKind.UseMove => ActionKind.UseMove.ToString(),
            server.domain.battle.enums.BattleActionKind.Prayer => ActionKind.Prayer.ToString(),
            server.domain.battle.enums.BattleActionKind.Guard => ActionKind.Guard.ToString(),
            server.domain.battle.enums.BattleActionKind.Wait => ActionKind.Wait.ToString(),
            _ => actionKind.ToString()
        };
    }

    private static IReadOnlyList<string> BuildActionLogs(
        string actorDisplayName,
        string actionKind,
        string? moveName,
        bool succeeded,
        BattleActionFailureReason? failureReason,
        IReadOnlyList<QuestResolvedTargetSummary> targetSummaries)
    {
        if (!succeeded)
        {
            return [BuildFailureLog(actorDisplayName, actionKind, moveName, failureReason)];
        }

        if (targetSummaries.Count == 0)
        {
            return actionKind switch
            {
                nameof(ActionKind.Guard) => [$"{actorDisplayName}は身を守っている"],
                nameof(ActionKind.Wait) => [$"{actorDisplayName}は様子を見ている"],
                _ => [$"{actorDisplayName}は行動した"]
            };
        }

        return targetSummaries
            .GroupBy(target => new
            {
                target.TargetParticipantId,
                target.TargetEnemyInstanceId,
                target.TargetDisplayName
            })
            .Select(group => new QuestResolvedTargetSummary(
                group.Key.TargetParticipantId,
                group.Key.TargetEnemyInstanceId,
                group.Key.TargetDisplayName,
                group.Any(target => target.ResultType == "Defeated")
                    ? "Defeated"
                    : group.Any(target => target.ResultType == "AilmentApplied")
                        ? "AilmentApplied"
                        : group.Any(target => target.ResultType == "Hit")
                            ? "Hit"
                            : "Miss",
                group.Sum(target => target.HpChange),
                group.Sum(target => target.MpChange),
                group.SelectMany(target => target.AppliedEffects).Distinct().ToArray(),
                group.SelectMany(target => target.RemovedEffects).Distinct().ToArray(),
                group.Any(target => target.IsDeadAfterAction)))
            .Select(target => BuildTargetLog(actorDisplayName, actionKind, moveName, target))
            .ToArray();
    }

    private static string BuildFailureLog(
        string actorDisplayName,
        string actionKind,
        string? moveName,
        BattleActionFailureReason? failureReason)
    {
        var actionLabel = actionKind == nameof(ActionKind.UseMove) && !string.IsNullOrWhiteSpace(moveName)
            ? moveName
            : actionKind switch
            {
                nameof(ActionKind.NormalAttack) => "攻撃",
                nameof(ActionKind.Prayer) => "祈り",
                nameof(ActionKind.Guard) => "防御",
                _ => "行動"
            };

        return failureReason switch
        {
            BattleActionFailureReason.ActorUnavailable => $"{actorDisplayName}は行動前に倒れた",
            BattleActionFailureReason.NoTarget => $"{actorDisplayName}は{actionLabel}しようとしたが、対象がいなかった",
            BattleActionFailureReason.Paralyzed => $"{actorDisplayName}は麻痺して動けなかった",
            BattleActionFailureReason.CannotAct => $"{actorDisplayName}は行動できなかった",
            BattleActionFailureReason.InsufficientMp => $"{actorDisplayName}はMPが足りず{actionLabel}できなかった",
            BattleActionFailureReason.MoveUnavailable => $"{actorDisplayName}は{actionLabel}できなかった",
            _ => $"{actorDisplayName}は行動したが失敗した"
        };
    }

    private static string BuildTargetLog(
        string actorDisplayName,
        string actionKind,
        string? moveName,
        QuestResolvedTargetSummary target)
    {
        if (actionKind == nameof(ActionKind.Prayer))
        {
            return $"{actorDisplayName}は{target.TargetDisplayName}に祈りを捧げた";
        }

        if (target.HpChange < 0)
        {
            var damage = Math.Abs(target.HpChange);
            if (target.AppliedEffects.Count > 0)
            {
                var effectNames = string.Join("、", target.AppliedEffects);
                return actionKind == nameof(ActionKind.UseMove) && !string.IsNullOrWhiteSpace(moveName)
                    ? $"{actorDisplayName}は{target.TargetDisplayName}に{moveName}を使って{damage}ダメージを与え、{effectNames}を付与した"
                    : $"{actorDisplayName}は{target.TargetDisplayName}に{damage}ダメージを与え、{effectNames}を付与した";
            }

            return actionKind == nameof(ActionKind.UseMove) && !string.IsNullOrWhiteSpace(moveName)
                ? $"{actorDisplayName}は{target.TargetDisplayName}に{moveName}を使って{damage}ダメージを与えた"
                : $"{actorDisplayName}は{target.TargetDisplayName}に{damage}ダメージを与えた";
        }

        if (target.HpChange > 0)
        {
            return actionKind == nameof(ActionKind.UseMove) && !string.IsNullOrWhiteSpace(moveName)
                ? $"{actorDisplayName}は{target.TargetDisplayName}に{moveName}を使って{target.HpChange}回復した"
                : $"{actorDisplayName}は{target.TargetDisplayName}を{target.HpChange}回復した";
        }

        if (target.MpChange > 0)
        {
            return actionKind == nameof(ActionKind.UseMove) && !string.IsNullOrWhiteSpace(moveName)
                ? $"{actorDisplayName}は{target.TargetDisplayName}に{moveName}を使ってMPを{target.MpChange}回復した"
                : $"{actorDisplayName}は{target.TargetDisplayName}のMPを{target.MpChange}回復した";
        }

        if (target.AppliedEffects.Count > 0)
        {
            var effectNames = string.Join("、", target.AppliedEffects);
            return actionKind == nameof(ActionKind.UseMove) && !string.IsNullOrWhiteSpace(moveName)
                ? $"{actorDisplayName}は{target.TargetDisplayName}に{moveName}を使って{effectNames}を付与した"
                : $"{actorDisplayName}は{target.TargetDisplayName}に{effectNames}を付与した";
        }

        return actionKind == nameof(ActionKind.UseMove) && !string.IsNullOrWhiteSpace(moveName)
            ? $"{actorDisplayName}は{target.TargetDisplayName}に{moveName}を使った"
            : $"{actorDisplayName}は{target.TargetDisplayName}に行動した";
    }

    private sealed record ActorStateSnapshot(int CurrentHp, int CurrentMp, bool IsDead);
}
