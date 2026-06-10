using server.application.battle;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.pet;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using server.domain.treasuremap;
using server.domain.treasuremap.enums;

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
    IItemRepository itemRepository,
    IJobProfileRepository jobProfileRepository,
    IJobMoveLearningRuleRepository jobMoveLearningRuleRepository,
    ITreasureMapExpeditionRepository treasureMapExpeditionRepository,
    IPlayerPetRepository playerPetRepository,
    QuestPetActionService questPetActionService,
    BattleService battleService,
    QuestBattleFactory questBattleFactory,
    Func<int, int>? rewardRollProvider = null,
    Func<int, int>? petRollProvider = null)
{
    private static readonly TimeSpan TurnDeadline = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan QuestCooldown = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan QuestCompletionTreasureMapAdvance = TimeSpan.FromMinutes(5);
    private const int ItemCapacity = 20;
    private readonly Func<int, int> _rewardRollProvider = rewardRollProvider ?? (maxInclusive => Random.Shared.Next(1, maxInclusive + 1));
    private readonly Func<int, int> _petRollProvider = petRollProvider ?? (maxInclusive => Random.Shared.Next(1, maxInclusive + 1));

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

        var previousFloorNo = run.FloorState.CurrentFloorNo;
        var previousStatus = run.Status.ToString();

        var captureActions = await ProcessCaptureCommandsAsync(run, enemyDefinitions);

        var fieldContext = questBattleFactory.CreateBattleFieldContext(run);
        var actors = questBattleFactory.CreateActorInputs(run, enemyDefinitions);
        var (actions, moves) = await questBattleFactory.CreateTurnInputsAsync(run, moveRepository, enemyDefinitions);

        var petSummons = await CreatePetSummonInputsAsync(run, enemyDefinitions);
        if (petSummons.Actors.Length > 0)
        {
            actors = [.. actors, .. petSummons.Actors];
            actions = [.. actions, .. petSummons.Actions];
            moves = MergeMoves(moves, petSummons.Moves);
        }

        var resolution = battleService.ResolveTurn(new BattleTurnRequest(actors, actions, moves, fieldContext));
        var resolvedMoves = await LoadMissingAilmentSourceMovesAsync(moves, resolution, moveRepository);

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
            resolvedMoves,
            enemyDefinitions,
            partyActorMap,
            enemyActorMap,
            petSummons.ActorNames,
            captureActions,
            previousFloorNo,
            previousStatus,
            nextFloor?.FloorNo,
            nextFloor is not null && nextFloor.FloorType == FloorType.Boss,
            summary));

        if (summary.IsFloorCleared)
        {
            var capturedEnemies = run.BattleState.Enemies.Where(x => x.IsCaptured).ToArray();
            run.Rewards.AddExp(CalculateFloorExp(currentFloor, enemyDefinitions, capturedEnemies));
            run.Rewards.AddGold(CalculateFloorGold(currentFloor, enemyDefinitions, capturedEnemies));
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
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions,
        IReadOnlyList<QuestEnemyState>? capturedEnemies = null)
    {
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(enemyDefinitions);

        var baseExp = floor.Placements.Sum(placement =>
            enemyDefinitions.TryGetValue(placement.EnemyDefinitionId, out var definition)
                ? definition.Level
                : 0);
        baseExp = Math.Max(0, baseExp - SumCapturedEnemyLevels(enemyDefinitions, capturedEnemies));

        return (int)Math.Floor(baseExp * floor.RewardRule.ExpRate);
    }

    private static int CalculateFloorGold(
        QuestFloorDefinition floor,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions,
        IReadOnlyList<QuestEnemyState>? capturedEnemies = null)
    {
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(enemyDefinitions);

        var baseGold = floor.Placements.Sum(placement =>
            enemyDefinitions.TryGetValue(placement.EnemyDefinitionId, out var definition)
                ? definition.Level
                : 0);
        baseGold = Math.Max(0, baseGold - SumCapturedEnemyLevels(enemyDefinitions, capturedEnemies));

        return (int)Math.Floor(baseGold * floor.RewardRule.GoldRate);
    }

    private static int SumCapturedEnemyLevels(
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions,
        IReadOnlyList<QuestEnemyState>? capturedEnemies)
    {
        if (capturedEnemies is null || capturedEnemies.Count == 0)
        {
            return 0;
        }

        return capturedEnemies.Sum(enemy =>
            enemyDefinitions.TryGetValue(enemy.EnemyDefinitionId, out var definition)
                ? definition.Level
                : 0);
    }

    private async Task<IReadOnlyList<QuestResolvedAction>> ProcessCaptureCommandsAsync(
        QuestRun run,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions)
    {
        var captureCommands = run.TurnState.PendingCommands
            .Where(x => x.ActionKind == ActionKind.Capture)
            .OrderBy(x => x.SubmittedAt)
            .ToArray();
        if (captureCommands.Length == 0)
        {
            return [];
        }

        var room = await questRoomRepository.GetAsync(run.RoomId)
            ?? throw new KeyNotFoundException($"ルームが見つかりません。 roomId={run.RoomId.Value}");
        var snapshotById = run.PartySnapshots.ToDictionary(x => x.ParticipantId);
        var logActions = new List<QuestResolvedAction>();

        foreach (var command in captureCommands)
        {
            var member = run.BattleState.FindPartyMember(command.ParticipantId);
            if (member.IsDead || member.HasLeftQuest)
            {
                continue;
            }

            var actorName = snapshotById.TryGetValue(command.ParticipantId, out var snapshot)
                ? snapshot.DisplayName
                : command.ParticipantId.Value.ToString();

            var target = command.SelectedTargetPosition is null
                ? null
                : run.BattleState.Enemies.FirstOrDefault(x => x.IsAlive && x.Position == command.SelectedTargetPosition.Value);
            if (target is null)
            {
                logActions.Add(CreateCaptureLogAction(
                    command.ParticipantId,
                    actorName,
                    succeeded: false,
                    CreateLogEntry((actorName, LogTone.Default), ("は捕獲しようとしたが、対象がいなかった", LogTone.Default))));
                continue;
            }

            enemyDefinitions.TryGetValue(target.EnemyDefinitionId, out var definition);
            var targetName = definition?.Name ?? target.EnemyDefinitionId.ToString();

            var playerId = room.Participants.FirstOrDefault(x => x.Id == command.ParticipantId)?.PlayerId;
            if (playerId is null || definition is null)
            {
                logActions.Add(CreateCaptureLogAction(
                    command.ParticipantId,
                    actorName,
                    succeeded: false,
                    CreateLogEntry((actorName, LogTone.Default), ("は", LogTone.Default), (targetName, LogTone.Default), ("を捕獲できなかった", LogTone.Default))));
                continue;
            }

            var petCount = await playerPetRepository.CountByPlayerAsync(playerId.Value);
            if (petCount >= PetConstants.MaxPetCount)
            {
                logActions.Add(CreateCaptureLogAction(
                    command.ParticipantId,
                    actorName,
                    succeeded: false,
                    CreateLogEntry((actorName, LogTone.Default), ("はこれ以上ペットを所持できない", LogTone.Default))));
                continue;
            }

            var player = await playerRepository.GetPlayerAsync(playerId.Value)
                ?? throw new KeyNotFoundException($"プレイヤーが見つかりません。 playerId={playerId.Value.Value}");
            var rate = PetCaptureRateCalculator.Calculate(player.Level, definition.Level);
            if (_petRollProvider(100) <= rate)
            {
                target.MarkCaptured();
                await playerPetRepository.AddAsync(PlayerPet.Capture(playerId.Value, definition.Id, DateTimeOffset.UtcNow));
                logActions.Add(CreateCaptureLogAction(
                    command.ParticipantId,
                    actorName,
                    succeeded: true,
                    CreateLogEntry((actorName, LogTone.Default), ("は", LogTone.Default), (targetName, LogTone.Buff), ("を捕まえた！", LogTone.Buff))));
            }
            else
            {
                logActions.Add(CreateCaptureLogAction(
                    command.ParticipantId,
                    actorName,
                    succeeded: false,
                    CreateLogEntry((actorName, LogTone.Default), ("は", LogTone.Default), (targetName, LogTone.Default), ("の捕獲に失敗した", LogTone.Default))));
            }
        }

        return logActions;
    }

    private static QuestResolvedAction CreateCaptureLogAction(
        QuestParticipantId participantId,
        string actorDisplayName,
        bool succeeded,
        QuestBattleLogEntry entry)
    {
        return new QuestResolvedAction(
            participantId.Value,
            actorEnemyInstanceId: null,
            actorDisplayName,
            nameof(ActionKind.Capture),
            moveId: null,
            moveName: null,
            succeeded,
            logs: [entry.Text],
            logEntries: [entry]);
    }

    private async Task<(BattleActorInput[] Actors, BattleActionInput[] Actions, Move[] Moves, IReadOnlyDictionary<BattleActorId, string> ActorNames)> CreatePetSummonInputsAsync(
        QuestRun run,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions)
    {
        var summonCommands = run.TurnState.PendingCommands
            .Where(x => x.ActionKind == ActionKind.SummonPet)
            .ToArray();
        if (summonCommands.Length == 0)
        {
            return ([], [], [], new Dictionary<BattleActorId, string>());
        }

        var snapshotById = run.PartySnapshots.ToDictionary(x => x.ParticipantId);
        var actors = new List<BattleActorInput>();
        var actions = new List<BattleActionInput>();
        var movesById = new Dictionary<int, Move>();
        var actorNames = new Dictionary<BattleActorId, string>();

        foreach (var command in summonCommands)
        {
            var member = run.BattleState.FindPartyMember(command.ParticipantId);
            if (member.IsDead || member.HasLeftQuest || !member.HasRemainingPetSummons)
            {
                continue;
            }

            if (!snapshotById.TryGetValue(command.ParticipantId, out var snapshot) || snapshot.Pet is null)
            {
                continue;
            }

            member.ConsumePetSummon();

            enemyDefinitions.TryGetValue(snapshot.Pet.EnemyDefinitionId, out var definition);
            var petMoves = new List<Move>();
            foreach (var moveId in definition?.MoveIds ?? [])
            {
                var move = await moveRepository.GetMoveAsync(moveId);
                if (move is not null)
                {
                    petMoves.Add(move);
                    movesById[move.Id.Id] = move;
                }
            }

            var petName = definition?.Name ?? "ペット";
            var displayName = $"{snapshot.DisplayName}の{petName}";
            var (actor, action, _) = questPetActionService.CreateSummonInputs(
                Guid.NewGuid(),
                displayName,
                snapshot.Pet,
                petMoves,
                _petRollProvider);
            actors.Add(actor);
            actions.Add(action);
            actorNames[new BattleActorId(actor.ActorId)] = displayName;
        }

        return (actors.ToArray(), actions.ToArray(), movesById.Values.ToArray(), actorNames);
    }

    private static Move[] MergeMoves(IReadOnlyList<Move> baseMoves, IReadOnlyList<Move> additionalMoves)
    {
        var movesById = baseMoves
            .GroupBy(x => x.Id.Id)
            .ToDictionary(x => x.Key, x => x.First());
        foreach (var move in additionalMoves)
        {
            movesById[move.Id.Id] = move;
        }

        return movesById.Values.ToArray();
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

        var hasGreatThiefBonus = room.Participants
            .Where(participant => participant.Type == ParticipantType.Player)
            .Where(participant =>
            {
                var state = run.BattleState.FindPartyMember(participant.Id);
                return !state.HasLeftQuest;
            })
            .Join(
                run.PartySnapshots,
                participant => participant.Id,
                snapshot => snapshot.ParticipantId,
                (_, snapshot) => snapshot.Job)
            .Any(job => job == Job.GreatThief);

        var rewardEquipmentId = run.Status == QuestRunStatus.Succeeded
            ? DrawEquipmentReward(stage, hasGreatThiefBonus)
            : null;
        var rewardItemId = run.Status == QuestRunStatus.Succeeded
            ? DrawItemReward(stage, hasGreatThiefBonus)
            : null;
        run.Rewards.SetEquipmentReward(rewardEquipmentId);
        run.Rewards.SetItemReward(rewardItemId);
        var skippedRewardPlayerIds = new List<PlayerId>();

        foreach (var playerId in rewardedPlayerIds)
        {
            var player = await playerRepository.GetPlayerAsync(playerId)
                ?? throw new KeyNotFoundException($"プレイヤーが見つかりません。 playerId={playerId.Value}");

            if (run.Rewards.Exp > 0)
            {
                var multiplier = ExpMultiplierFlag.ToMultiplier(player.ExpMultiplierFlags);
                var multipliedExp = (int)Math.Floor(run.Rewards.Exp * multiplier);
                player.ClearExpMultiplierFlags();
                player.GainExp(multipliedExp);
                var jobProfile = jobProfileRepository.GetByJob(player.Job);
                var learningRule = jobMoveLearningRuleRepository.GetByJob(player.Job);
                player.LevelUp(jobProfile, learningRule);
            }
            else
            {
                player.ClearExpMultiplierFlags();
            }

            if (stage.RequiredMapUnlockFlag is not null && playerId == room.OwnerId)
            {
                player.ClearMapUnlockFlag(stage.RequiredMapUnlockFlag.Value);
            }

            if (run.Rewards.Gold > 0)
            {
                player.GainGold(run.Rewards.Gold);
            }

            player.SetQuestCooldownUntil((run.EndedAt ?? DateTimeOffset.UtcNow).Add(QuestCooldown));
            await playerRepository.SaveAsync(player);

            if (run.Status == QuestRunStatus.Succeeded)
            {
                var expedition = await treasureMapExpeditionRepository.GetCurrentByPlayerAsync(playerId);
                if (expedition is not null && expedition.Status == TreasureMapExpeditionStatus.InProgress)
                {
                    expedition.AdvanceTime(QuestCompletionTreasureMapAdvance);
                    await treasureMapExpeditionRepository.SaveAsync(expedition);
                }
            }

            if (run.Status == QuestRunStatus.Succeeded && (rewardEquipmentId is not null || rewardItemId is not null))
            {
                var playerEquipments = (await playerEquipmentRepository.GetByPlayerAsync(playerId)).ToList();
                var playerItemStacks = (await playerItemStackRepository.GetByPlayerAsync(playerId)).ToList();
                var listings = await marketListingRepository.GetBySellerAsync(playerId, DateTimeOffset.UtcNow);
                var listedEquipmentIds = listings
                    .Where(x => x.PlayerEquipmentId is not null)
                    .Select(x => x.PlayerEquipmentId!.Value)
                    .ToHashSet();
                var usedSlots = playerEquipments.Count(x => x.Status != EquipmentStatus.Equipped && !listedEquipmentIds.Contains(x.Id))
                                + playerItemStacks.Count;

                if (rewardEquipmentId is not null)
                {
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
                            0,
                            rewardGrantedAt,
                            rewardGrantedAt));
                        usedSlots += 1;
                        await playerEquipmentRepository.SaveAsync(playerEquipments);
                    }
                }

                if (rewardItemId is not null)
                {
                    var rewardItem = await itemRepository.GetAsync(rewardItemId.Value)
                        ?? throw new KeyNotFoundException($"アイテムマスタが見つかりません。 itemId={rewardItemId.Value.Value}");
                    var existingStack = playerItemStacks.FirstOrDefault(stack => stack.ItemId == rewardItem.Id);
                    if (existingStack is not null)
                    {
                        existingStack.AddQuantity(1, rewardItem.MaxStack, DateTimeOffset.UtcNow);
                        await playerItemStackRepository.SaveAsync(playerItemStacks);
                    }
                    else if (usedSlots >= ItemCapacity)
                    {
                        skippedRewardPlayerIds.Add(playerId);
                    }
                    else
                    {
                        playerItemStacks.Add(new PlayerItemStack(
                            PlayerItemStackId.New(),
                            playerId,
                            rewardItem.Id,
                            quantity: 1,
                            DateTimeOffset.UtcNow));
                        await playerItemStackRepository.SaveAsync(playerItemStacks);
                    }
                }
            }
        }

        run.Rewards.SetSkippedRewardPlayerIds(skippedRewardPlayerIds);
    }

    private EquipmentId? DrawEquipmentReward(QuestStageDefinition stage, bool hasGreatThiefBonus)
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

        var drawCount = hasGreatThiefBonus ? 2 : 1;
        for (var i = 0; i < drawCount; i++)
        {
            var roll = _rewardRollProvider(totalWeight);
            var cumulative = 0;
            foreach (var entry in stage.EquipmentRewards)
            {
                cumulative += entry.Weight;
                if (roll > cumulative)
                {
                    continue;
                }

                if (!entry.IsMiss)
                {
                    return entry.EquipmentId;
                }

                break;
            }
        }

        return null;
    }

    private ItemId? DrawItemReward(QuestStageDefinition stage, bool hasGreatThiefBonus)
    {
        if (stage.ItemRewards.Count == 0)
        {
            return null;
        }

        var totalWeight = stage.ItemRewards.Sum(x => x.Weight);
        if (totalWeight <= 0)
        {
            return null;
        }

        var drawCount = hasGreatThiefBonus ? 2 : 1;
        for (var i = 0; i < drawCount; i++)
        {
            var roll = _rewardRollProvider(totalWeight);
            var cumulative = 0;
            foreach (var entry in stage.ItemRewards)
            {
                cumulative += entry.Weight;
                if (roll > cumulative)
                {
                    continue;
                }

                if (!entry.IsMiss)
                {
                    return entry.ItemId;
                }

                break;
            }
        }

        return null;
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
        IReadOnlyDictionary<BattleActorId, string> petActorNames,
        IReadOnlyList<QuestResolvedAction> captureActions,
        int previousFloorNo,
        string previousStatus,
        int? nextFloorNo,
        bool nextFloorIsBoss,
        QuestRunResolutionSummary summary)
    {
        var moveById = moves
            .GroupBy(x => x.Id.Id)
            .ToDictionary(x => x.Key, x => x.First());
        var snapshotByParticipantId = run.PartySnapshots.ToDictionary(x => x.ParticipantId);
        var partyById = run.BattleState.PartyMembers.ToDictionary(x => x.ParticipantId);
        var enemyById = run.BattleState.Enemies.ToDictionary(x => x.Id);

        var actions = resolution.ActionResults
            .Where(actionResult => !actionResult.IsTurnEndEffect)
            .Select(actionResult =>
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
            else if (petActorNames.TryGetValue(actionResult.ActorId, out var petActorName))
            {
                actorDisplayName = petActorName;
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
                else if (petActorNames.TryGetValue(targetResult.TargetActorId, out var petTargetName))
                {
                    targetDisplayName = petTargetName;
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
        })
        .ToArray();

        var ailmentLogActions = BuildAilmentTickActions(
            resolution,
            moveById,
            partyActorMap,
            enemyActorMap,
            snapshotByParticipantId,
            partyById,
            enemyById,
            enemyDefinitions);

        actions = [.. captureActions, .. actions, .. ailmentLogActions];

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

    private static async Task<IReadOnlyList<Move>> LoadMissingAilmentSourceMovesAsync(
        IReadOnlyList<Move> moves,
        BattleTurnResolution resolution,
        IMoveRepository moveRepository)
    {
        var moveById = moves.ToDictionary(move => move.Id);
        var missingMoveIds = resolution.ActionResults
            .Where(result => result.IsTurnEndEffect)
            .SelectMany(result => result.TargetResults)
            .Select(result => result.SourceMoveId)
            .OfType<MoveId>()
            .Where(moveId => !moveById.ContainsKey(moveId))
            .Distinct()
            .ToArray();

        if (missingMoveIds.Length == 0)
        {
            return moves;
        }

        var resolvedMoves = new List<Move>(moves);
        foreach (var moveId in missingMoveIds)
        {
            var move = await moveRepository.GetMoveAsync(moveId);
            if (move is not null)
            {
                resolvedMoves.Add(move);
            }
        }

        return resolvedMoves;
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
            BattleActionFailureReason.Sleeping => CreateLogEntry((actorDisplayName, LogTone.Default), ("は眠っていて動けなかった", LogTone.Default)),
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
        IReadOnlyDictionary<QuestParticipantId, QuestRunPartyMemberSnapshot> snapshotByParticipantId,
        IReadOnlyDictionary<QuestParticipantId, QuestRunPartyMemberState> partyById,
        IReadOnlyDictionary<QuestEnemyInstanceId, QuestEnemyState> enemyById,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions)
    {
        var logActions = new List<QuestResolvedAction>();

        foreach (var targetResult in resolution.ActionResults
                     .Where(result => result.IsTurnEndEffect)
                     .SelectMany(result => result.TargetResults))
        {
            if (targetResult.TriggeredAilment is not AilmentType.DamageTrap
                and not AilmentType.PoisonTrap
                and not AilmentType.Poison
                and not AilmentType.Burn)
            {
                continue;
            }

            var sourceMoveId = targetResult.SourceMoveId;
            var sourceMove = sourceMoveId is null ? null : moveById.GetValueOrDefault(sourceMoveId.Id);
            var (actorParticipantId, actorEnemyInstanceId, actorDisplayName) = ResolveActorInfo(
                targetResult.TargetActorId,
                partyActorMap,
                enemyActorMap,
                snapshotByParticipantId,
                partyById,
                enemyById,
                enemyDefinitions);
            var entry = BuildAilmentTickLog(actorDisplayName, targetResult.Damage, targetResult.TriggeredAilment.Value, sourceMove);

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
            AilmentType.Burn => "やけど",
            _ => ailmentType.ToString()
        };

        return sourceMove is not null
            ? CreateLogEntry(
                (actorDisplayName, LogTone.Default),
                ("は", LogTone.Default),
                (sourceMove.Name, ailmentType is AilmentType.DamageTrap or AilmentType.PoisonTrap or AilmentType.Poison or AilmentType.Burn ? LogTone.Ailment : ResolveMoveTone(sourceMove)),
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
                effect.Ailment.AilmentType is AilmentType.Poison or AilmentType.PoisonTrap or AilmentType.DamageTrap or AilmentType.Burn or AilmentType.Sleep or AilmentType.InstantDeath))
        {
            return LogTone.Ailment;
        }

        if (move.Effects.Any(effect => effect.EffectType == MoveEffectType.Damage))
        {
            return LogTone.Damage;
        }

        return LogTone.Default;
    }

    private static class LogTone
    {
        public const string Default = "Default";
        public const string Damage = "Damage";
        public const string Ailment = "Ailment";
        public const string Buff = "Buff";
        public const string Heal = "Heal";
    }
}
