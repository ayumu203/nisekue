using server.application.battle;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
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
    IPlayerEquipmentRepository playerEquipmentRepository,
    IPlayerItemStackRepository playerItemStackRepository,
    IMarketListingRepository marketListingRepository,
    IEquipmentRepository equipmentRepository,
    IJobProfileRepository jobProfileRepository,
    IJobMoveLearningRuleRepository jobMoveLearningRuleRepository,
    BattleService battleService,
    QuestBattleFactory questBattleFactory)
{
    private static readonly TimeSpan TurnDeadline = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan QuestCooldown = TimeSpan.FromMinutes(3);
    private const int ItemCapacity = 20;

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
            x => new ActorStateSnapshot(x.CurrentHp, x.CurrentMp, x.IsDead, x.Ailments));
        var beforeEnemy = run.BattleState.Enemies.ToDictionary(
            x => x.Id,
            x => new ActorStateSnapshot(x.CurrentHp, x.CurrentMp, x.IsDead, x.Ailments));
        var previousFloorNo = run.FloorState.CurrentFloorNo;
        var previousStatus = run.Status.ToString();

        var fieldContext = questBattleFactory.CreateBattleFieldContext(run);
        var actors = questBattleFactory.CreateActorInputs(run, enemyDefinitions);
        var (actions, moves) = await questBattleFactory.CreateTurnInputsAsync(run, moveRepository, enemyDefinitions);
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
        var stage = await questStageRepository.GetAsync(run.StageId)
            ?? throw new KeyNotFoundException($"ステージ定義が見つかりません。 stageId={run.StageId.Value}");

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

        var rewardEquipmentId = run.Status == QuestRunStatus.Succeeded
            ? DrawEquipmentReward(stage)
            : null;
        run.Rewards.SetEquipmentReward(rewardEquipmentId);
        var skippedRewardPlayerIds = new List<PlayerId>();

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

            if (run.Status == QuestRunStatus.Succeeded && rewardEquipmentId is not null)
            {
                var playerEquipments = (await playerEquipmentRepository.GetByPlayerAsync(playerId)).ToList();
                var playerItemStacks = await playerItemStackRepository.GetByPlayerAsync(playerId);
                var listings = await marketListingRepository.GetBySellerAsync(playerId, DateTimeOffset.UtcNow);
                var listedEquipmentIds = listings
                    .Where(x => x.PlayerEquipmentId is not null)
                    .Select(x => x.PlayerEquipmentId!.Value)
                    .ToHashSet();
                var usedSlots = playerEquipments.Count(x => x.Status != EquipmentStatus.Equipped && !listedEquipmentIds.Contains(x.Id))
                                + playerItemStacks.Count;

                if (usedSlots >= ItemCapacity)
                {
                    skippedRewardPlayerIds.Add(playerId);
                }
                else
                {
                    var rewardMaster = await equipmentRepository.GetAsync(rewardEquipmentId.Value)
                        ?? throw new KeyNotFoundException($"装備マスタが見つかりません。 equipmentId={rewardEquipmentId.Value.Value}");
                    var rewardGrantedAt = run.EndedAt ?? DateTimeOffset.UtcNow;
                    playerEquipments.Add(new PlayerEquipment(
                        PlayerEquipmentId.New(),
                        playerId,
                        rewardMaster.Id,
                        rewardMaster.Type,
                        EquipmentStatus.Inventory,
                        rewardMaster.MaxDurability,
                        0,
                        rewardGrantedAt,
                        rewardGrantedAt));
                    await playerEquipmentRepository.SaveAsync(playerEquipments);
                }
            }
        }

        run.Rewards.SetSkippedRewardPlayerIds(skippedRewardPlayerIds);

        var now = run.EndedAt ?? DateTimeOffset.UtcNow;
        foreach (var participant in room.Participants.Where(x => x.PlayerId is not null && x.Type == ParticipantType.Player))
        {
            var snapshot = run.PartySnapshots.FirstOrDefault(x => x.ParticipantId == participant.Id);
            if (snapshot is null)
            {
                continue;
            }

            var playerEquipments = (await playerEquipmentRepository.GetByPlayerAsync(participant.PlayerId!.Value)).ToList();
            var consumed = false;
            consumed |= ConsumeEquipmentDurability(playerEquipments, snapshot.WeaponEquipmentId, now);
            consumed |= ConsumeEquipmentDurability(playerEquipments, snapshot.ArmorEquipmentId, now);

            if (consumed)
            {
                await playerEquipmentRepository.SaveAsync(playerEquipments);
            }
        }
    }

    private static EquipmentId? DrawEquipmentReward(QuestStageDefinition stage)
    {
        if (stage.EquipmentRewards.Count == 0)
        {
            return null;
        }

        var totalWeight = stage.EquipmentRewards.Sum(x => x.Weight);
        if (totalWeight <= 0)
        {
            return null;
        }

        var roll = Random.Shared.Next(1, totalWeight + 1);
        var cumulative = 0;
        foreach (var entry in stage.EquipmentRewards)
        {
            cumulative += entry.Weight;
            if (roll > cumulative)
            {
                continue;
            }

            return entry.IsMiss ? null : entry.EquipmentId;
        }

        return null;
    }

    private static bool ConsumeEquipmentDurability(
        IReadOnlyList<PlayerEquipment> playerEquipments,
        PlayerEquipmentId? playerEquipmentId,
        DateTimeOffset now)
    {
        if (playerEquipmentId is null)
        {
            return false;
        }

        var equipment = playerEquipments.FirstOrDefault(x => x.Id == playerEquipmentId.Value);
        if (equipment is null)
        {
            return false;
        }

        equipment.ConsumeDurability(1, now);
        return true;
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

            var move = actionResult.MoveId is null ? null : moveById.GetValueOrDefault(actionResult.MoveId.Id);
            var logEntries = BuildActionLogs(
                actorDisplayName,
                MapActionKind(actionResult.ActionKind),
                move,
                actionResult.Succeeded,
                actionResult.FailureReason,
                targetSummaries);

            return new QuestResolvedAction(
                actorParticipantId,
                actorEnemyInstanceId,
                actorDisplayName,
                MapActionKind(actionResult.ActionKind),
                actionResult.MoveId?.Id,
                move?.Name,
                actionResult.Succeeded,
                targetSummaries,
                logEntries.Select(entry => entry.Text),
                logEntries);
        }).ToArray();

        var ailmentLogActions = BuildAilmentTickActions(
            resolution,
            moveById,
            partyActorMap,
            enemyActorMap,
            beforeParty,
            beforeEnemy,
            snapshotByParticipantId,
            partyById,
            enemyById,
            enemyDefinitions);

        actions = [.. actions, .. ailmentLogActions];

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

    private static IReadOnlyList<QuestBattleLogEntry> BuildActionLogs(
        string actorDisplayName,
        string actionKind,
        Move? move,
        bool succeeded,
        BattleActionFailureReason? failureReason,
        IReadOnlyList<QuestResolvedTargetSummary> targetSummaries)
    {
        if (!succeeded)
        {
            return [BuildFailureLog(actorDisplayName, actionKind, move, failureReason)];
        }

        if (targetSummaries.Count == 0)
        {
            return actionKind switch
            {
                nameof(ActionKind.Guard) => [CreateLogEntry((actorDisplayName, LogTone.Default), ("は身を守っている", LogTone.Default))],
                nameof(ActionKind.Wait) => [CreateLogEntry((actorDisplayName, LogTone.Default), ("は様子を見ている", LogTone.Default))],
                _ => [CreateLogEntry((actorDisplayName, LogTone.Default), ("は行動した", LogTone.Default))]
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
            .Select(target => BuildTargetLog(actorDisplayName, actionKind, move, target))
            .ToArray();
    }

    private static QuestBattleLogEntry BuildFailureLog(
        string actorDisplayName,
        string actionKind,
        Move? move,
        BattleActionFailureReason? failureReason)
    {
        var actionLabelSegments = actionKind == nameof(ActionKind.UseMove) && move is not null
            ? new[] { (move.Name, ResolveMoveTone(move)) }
            : actionKind switch
            {
                nameof(ActionKind.NormalAttack) => new[] { ("攻撃", LogTone.Default) },
                nameof(ActionKind.Prayer) => new[] { ("祈り", LogTone.Default) },
                nameof(ActionKind.Guard) => new[] { ("防御", LogTone.Default) },
                _ => new[] { ("行動", LogTone.Default) }
            };

        return failureReason switch
        {
            BattleActionFailureReason.ActorUnavailable => CreateLogEntry((actorDisplayName, LogTone.Default), ("は行動前に倒れた", LogTone.Default)),
            BattleActionFailureReason.NoTarget => CreateLogEntry([(actorDisplayName, LogTone.Default), ("は", LogTone.Default), .. actionLabelSegments, ("しようとしたが、対象がいなかった", LogTone.Default)]),
            BattleActionFailureReason.Paralyzed => CreateLogEntry((actorDisplayName, LogTone.Default), ("は麻痺して動けなかった", LogTone.Default)),
            BattleActionFailureReason.CannotAct => CreateLogEntry((actorDisplayName, LogTone.Default), ("は行動できなかった", LogTone.Default)),
            BattleActionFailureReason.InsufficientMp => CreateLogEntry([(actorDisplayName, LogTone.Default), ("はMPが足りず", LogTone.Default), .. actionLabelSegments, ("できなかった", LogTone.Default)]),
            BattleActionFailureReason.MoveUnavailable => CreateLogEntry([(actorDisplayName, LogTone.Default), ("は", LogTone.Default), .. actionLabelSegments, ("できなかった", LogTone.Default)]),
            _ => CreateLogEntry((actorDisplayName, LogTone.Default), ("は行動したが失敗した", LogTone.Default))
        };
    }

    private static QuestBattleLogEntry BuildTargetLog(
        string actorDisplayName,
        string actionKind,
        Move? move,
        QuestResolvedTargetSummary target)
    {
        if (actionKind == nameof(ActionKind.Prayer))
        {
            return CreateLogEntry(
                (actorDisplayName, LogTone.Default),
                ("は", LogTone.Default),
                (target.TargetDisplayName, LogTone.Default),
                ("に祈りを捧げた", LogTone.Default));
        }

        if (target.HpChange < 0)
        {
            var damage = Math.Abs(target.HpChange);
            if (target.AppliedEffects.Count > 0)
            {
                var effectNames = string.Join("、", target.AppliedEffects);
                return move is not null
                    ? CreateLogEntry(
                        (actorDisplayName, LogTone.Default),
                        ("は", LogTone.Default),
                        (target.TargetDisplayName, LogTone.Default),
                        ("に", LogTone.Default),
                        (move.Name, ResolveMoveTone(move)),
                        ("を使って", LogTone.Default),
                        (damage.ToString(), LogTone.Damage),
                        ("ダメージを与え、", LogTone.Default),
                        (effectNames, LogTone.Default),
                        ("を付与した", LogTone.Default))
                    : CreateLogEntry(
                        (actorDisplayName, LogTone.Default),
                        ("は", LogTone.Default),
                        (target.TargetDisplayName, LogTone.Default),
                        ("に", LogTone.Default),
                        (damage.ToString(), LogTone.Damage),
                        ("ダメージを与え、", LogTone.Default),
                        (effectNames, LogTone.Default),
                        ("を付与した", LogTone.Default));
            }

            return move is not null
                ? CreateLogEntry(
                    (actorDisplayName, LogTone.Default),
                    ("は", LogTone.Default),
                    (target.TargetDisplayName, LogTone.Default),
                    ("に", LogTone.Default),
                    (move.Name, ResolveMoveTone(move)),
                    ("を使って", LogTone.Default),
                    (damage.ToString(), LogTone.Damage),
                    ("ダメージを与えた", LogTone.Default))
                : CreateLogEntry(
                    (actorDisplayName, LogTone.Default),
                    ("は", LogTone.Default),
                    (target.TargetDisplayName, LogTone.Default),
                    ("に", LogTone.Default),
                    (damage.ToString(), LogTone.Damage),
                    ("ダメージを与えた", LogTone.Default));
        }

        if (target.HpChange > 0)
        {
            return move is not null
                ? CreateLogEntry(
                    (actorDisplayName, LogTone.Default),
                    ("は", LogTone.Default),
                    (target.TargetDisplayName, LogTone.Default),
                    ("に", LogTone.Default),
                    (move.Name, ResolveMoveTone(move)),
                    ("を使って", LogTone.Default),
                    (target.HpChange.ToString(), LogTone.Default),
                    ("回復した", LogTone.Default))
                : CreateLogEntry(
                    (actorDisplayName, LogTone.Default),
                    ("は", LogTone.Default),
                    (target.TargetDisplayName, LogTone.Default),
                    ("を", LogTone.Default),
                    (target.HpChange.ToString(), LogTone.Default),
                    ("回復した", LogTone.Default));
        }

        if (target.MpChange > 0)
        {
            return move is not null
                ? CreateLogEntry(
                    (actorDisplayName, LogTone.Default),
                    ("は", LogTone.Default),
                    (target.TargetDisplayName, LogTone.Default),
                    ("に", LogTone.Default),
                    (move.Name, ResolveMoveTone(move)),
                    ("を使ってMPを", LogTone.Default),
                    (target.MpChange.ToString(), LogTone.Default),
                    ("回復した", LogTone.Default))
                : CreateLogEntry(
                    (actorDisplayName, LogTone.Default),
                    ("は", LogTone.Default),
                    (target.TargetDisplayName, LogTone.Default),
                    ("のMPを", LogTone.Default),
                    (target.MpChange.ToString(), LogTone.Default),
                    ("回復した", LogTone.Default));
        }

        if (target.AppliedEffects.Count > 0)
        {
            var effectNames = string.Join("、", target.AppliedEffects);
            return move is not null
                ? CreateLogEntry(
                    (actorDisplayName, LogTone.Default),
                    ("は", LogTone.Default),
                    (target.TargetDisplayName, LogTone.Default),
                    ("に", LogTone.Default),
                    (move.Name, ResolveMoveTone(move)),
                    ("を使って", LogTone.Default),
                    (effectNames, LogTone.Default),
                    ("を付与した", LogTone.Default))
                : CreateLogEntry(
                    (actorDisplayName, LogTone.Default),
                    ("は", LogTone.Default),
                    (target.TargetDisplayName, LogTone.Default),
                    ("に", LogTone.Default),
                    (effectNames, LogTone.Default),
                    ("を付与した", LogTone.Default));
        }

        return move is not null
            ? CreateLogEntry(
                (actorDisplayName, LogTone.Default),
                ("は", LogTone.Default),
                (target.TargetDisplayName, LogTone.Default),
                ("に", LogTone.Default),
                (move.Name, ResolveMoveTone(move)),
                ("を使った", LogTone.Default))
            : CreateLogEntry(
                (actorDisplayName, LogTone.Default),
                ("は", LogTone.Default),
                (target.TargetDisplayName, LogTone.Default),
                ("に行動した", LogTone.Default));
    }

    private static IReadOnlyList<QuestResolvedAction> BuildAilmentTickActions(
        BattleTurnResolution resolution,
        IReadOnlyDictionary<int, Move> moveById,
        IReadOnlyDictionary<BattleActorId, QuestParticipantId> partyActorMap,
        IReadOnlyDictionary<BattleActorId, QuestEnemyInstanceId> enemyActorMap,
        IReadOnlyDictionary<QuestParticipantId, ActorStateSnapshot> beforeParty,
        IReadOnlyDictionary<QuestEnemyInstanceId, ActorStateSnapshot> beforeEnemy,
        IReadOnlyDictionary<QuestParticipantId, QuestRunPartyMemberSnapshot> snapshotByParticipantId,
        IReadOnlyDictionary<QuestParticipantId, QuestRunPartyMemberState> partyById,
        IReadOnlyDictionary<QuestEnemyInstanceId, QuestEnemyState> enemyById,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions)
    {
        var actionHpChanges = resolution.ActionResults
            .SelectMany(result => result.TargetResults)
            .GroupBy(result => result.TargetActorId)
            .ToDictionary(group => group.Key, group => group.Sum(result => result.HpChange));
        var appliedAilmentSources = resolution.ActionResults
            .Where(result => result.MoveId is not null)
            .SelectMany(result => result.TargetResults
                .Where(target => target.AppliedAilment is not null)
                .Select(target => new { target.TargetActorId, Ailment = target.AppliedAilment!.Value, result.MoveId }))
            .GroupBy(x => (x.TargetActorId, x.Ailment))
            .ToDictionary(group => group.Key, group => group.Last().MoveId!);
        var updatedStates = resolution.UpdatedStates.ToDictionary(state => state.Id);
        var logActions = new List<QuestResolvedAction>();

        foreach (var updatedState in updatedStates.Values)
        {
            var extraDamage = ResolveEndOfTurnDamage(updatedState, actionHpChanges, partyActorMap, enemyActorMap, beforeParty, beforeEnemy);
            if (extraDamage <= 0)
            {
                continue;
            }

            var beforeAilments = ResolveBeforeAilments(updatedState.Id, partyActorMap, enemyActorMap, beforeParty, beforeEnemy);
            var sourceAilment = updatedState.Ailments
                .Concat(beforeAilments)
                .FirstOrDefault(ailment => ailment.Type is AilmentType.DamageTrap or AilmentType.PoisonTrap or AilmentType.Poison);
            if (sourceAilment is null)
            {
                continue;
            }

            var sourceMoveId = sourceAilment.SourceMoveId
                ?? appliedAilmentSources.GetValueOrDefault((updatedState.Id, sourceAilment.Type));
            var sourceMove = sourceMoveId is null ? null : moveById.GetValueOrDefault(sourceMoveId.Id);
            var (actorParticipantId, actorEnemyInstanceId, actorDisplayName) = ResolveActorInfo(
                updatedState.Id,
                partyActorMap,
                enemyActorMap,
                snapshotByParticipantId,
                partyById,
                enemyById,
                enemyDefinitions);
            var entry = BuildAilmentTickLog(actorDisplayName, extraDamage, sourceAilment.Type, sourceMove);

            logActions.Add(new QuestResolvedAction(
                actorParticipantId,
                actorEnemyInstanceId,
                actorDisplayName,
                nameof(ActionKind.Wait),
                sourceMoveId?.Id,
                sourceMove?.Name,
                true,
                logs: [entry.Text],
                logEntries: [entry]));
        }

        return logActions;
    }

    private static int ResolveEndOfTurnDamage(
        BattleActorState updatedState,
        IReadOnlyDictionary<BattleActorId, int> actionHpChanges,
        IReadOnlyDictionary<BattleActorId, QuestParticipantId> partyActorMap,
        IReadOnlyDictionary<BattleActorId, QuestEnemyInstanceId> enemyActorMap,
        IReadOnlyDictionary<QuestParticipantId, ActorStateSnapshot> beforeParty,
        IReadOnlyDictionary<QuestEnemyInstanceId, ActorStateSnapshot> beforeEnemy)
    {
        int beforeHp;
        if (partyActorMap.TryGetValue(updatedState.Id, out var participantId))
        {
            beforeHp = beforeParty[participantId].CurrentHp;
        }
        else if (enemyActorMap.TryGetValue(updatedState.Id, out var enemyId))
        {
            beforeHp = beforeEnemy[enemyId].CurrentHp;
        }
        else
        {
            return 0;
        }

        var afterActionsHp = beforeHp + actionHpChanges.GetValueOrDefault(updatedState.Id);
        return Math.Max(0, afterActionsHp - updatedState.CurrentHp);
    }

    private static IReadOnlyList<BattleAilmentState> ResolveBeforeAilments(
        BattleActorId actorId,
        IReadOnlyDictionary<BattleActorId, QuestParticipantId> partyActorMap,
        IReadOnlyDictionary<BattleActorId, QuestEnemyInstanceId> enemyActorMap,
        IReadOnlyDictionary<QuestParticipantId, ActorStateSnapshot> beforeParty,
        IReadOnlyDictionary<QuestEnemyInstanceId, ActorStateSnapshot> beforeEnemy)
    {
        if (partyActorMap.TryGetValue(actorId, out var participantId))
        {
            return beforeParty[participantId].Ailments;
        }

        if (enemyActorMap.TryGetValue(actorId, out var enemyId))
        {
            return beforeEnemy[enemyId].Ailments;
        }

        return [];
    }

    private static (Guid? ActorParticipantId, Guid? ActorEnemyInstanceId, string ActorDisplayName) ResolveActorInfo(
        BattleActorId actorId,
        IReadOnlyDictionary<BattleActorId, QuestParticipantId> partyActorMap,
        IReadOnlyDictionary<BattleActorId, QuestEnemyInstanceId> enemyActorMap,
        IReadOnlyDictionary<QuestParticipantId, QuestRunPartyMemberSnapshot> snapshotByParticipantId,
        IReadOnlyDictionary<QuestParticipantId, QuestRunPartyMemberState> partyById,
        IReadOnlyDictionary<QuestEnemyInstanceId, QuestEnemyState> enemyById,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions)
    {
        if (partyActorMap.TryGetValue(actorId, out var participantId))
        {
            return (participantId.Value, null, snapshotByParticipantId[participantId].DisplayName);
        }

        if (enemyActorMap.TryGetValue(actorId, out var enemyInstanceId))
        {
            var enemy = enemyById[enemyInstanceId];
            var name = enemyDefinitions.TryGetValue(enemy.EnemyDefinitionId, out var definition)
                ? definition.Name
                : enemy.EnemyDefinitionId.ToString();
            return (null, enemyInstanceId.Value, name);
        }

        return (null, null, actorId.Value.ToString());
    }

    private static QuestBattleLogEntry BuildAilmentTickLog(
        string actorDisplayName,
        int damage,
        AilmentType ailmentType,
        Move? sourceMove)
    {
        var effectLabel = ailmentType switch
        {
            AilmentType.DamageTrap or AilmentType.PoisonTrap => "トラップ",
            AilmentType.Poison => "毒",
            _ => ailmentType.ToString()
        };

        return sourceMove is not null
            ? CreateLogEntry(
                (actorDisplayName, LogTone.Default),
                ("は", LogTone.Default),
                (sourceMove.Name, ailmentType is AilmentType.DamageTrap or AilmentType.PoisonTrap or AilmentType.Poison ? LogTone.Ailment : ResolveMoveTone(sourceMove)),
                ("による", LogTone.Default),
                (effectLabel, LogTone.Default),
                ("で", LogTone.Default),
                (damage.ToString(), LogTone.Damage),
                ("ダメージを受けた", LogTone.Default))
            : CreateLogEntry(
                (actorDisplayName, LogTone.Default),
                ("は", LogTone.Default),
                (effectLabel, LogTone.Default),
                ("で", LogTone.Default),
                (damage.ToString(), LogTone.Damage),
                ("ダメージを受けた", LogTone.Default));
    }

    private static QuestBattleLogEntry CreateLogEntry(params (string Text, string Tone)[] segments)
    {
        return CreateLogEntry((IEnumerable<(string Text, string Tone)>)segments);
    }

    private static QuestBattleLogEntry CreateLogEntry(IEnumerable<(string Text, string Tone)> segments)
    {
        var list = segments
            .Where(segment => !string.IsNullOrEmpty(segment.Text))
            .Select(segment => new QuestBattleLogSegment(segment.Text, segment.Tone))
            .ToArray();
        return new QuestBattleLogEntry(string.Concat(list.Select(segment => segment.Text)), list);
    }

    private static string ResolveMoveTone(Move move)
    {
        if (move.Effects.Any(effect => effect.EffectType == MoveEffectType.Heal || effect.EffectType == MoveEffectType.RestoreMp))
        {
            return LogTone.Heal;
        }

        if (move.Effects.Any(effect => effect.EffectType == MoveEffectType.Buff))
        {
            return LogTone.Buff;
        }

        if (move.Effects.Any(effect =>
                effect.EffectType == MoveEffectType.Ailment &&
                effect.Ailment is not null &&
                effect.Ailment.AilmentType is AilmentType.Poison or AilmentType.PoisonTrap or AilmentType.DamageTrap))
        {
            return LogTone.Ailment;
        }

        if (move.Effects.Any(effect => effect.EffectType == MoveEffectType.Damage))
        {
            return LogTone.Damage;
        }

        return LogTone.Default;
    }

    private sealed record ActorStateSnapshot(int CurrentHp, int CurrentMp, bool IsDead, IReadOnlyList<BattleAilmentState> Ailments);

    private static class LogTone
    {
        public const string Default = "Default";
        public const string Damage = "Damage";
        public const string Ailment = "Ailment";
        public const string Buff = "Buff";
        public const string Heal = "Heal";
    }
}
