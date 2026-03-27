using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestResponseMapper(
    IQuestStageRepository questStageRepository,
    IQuestEnemyDefinitionRepository questEnemyDefinitionRepository,
    IPlayerRepository playerRepository)
{
    public async Task<object> MapQuestStageSummaryAsync(QuestStageDefinition stage)
    {
        var enemyDefinitions = (await questEnemyDefinitionRepository.GetAllAsync())
            .ToDictionary(x => x.Id);

        return MapQuestStageSummary(stage, enemyDefinitions);
    }

    public async Task<IReadOnlyList<object>> MapQuestStageSummariesAsync(IEnumerable<QuestStageDefinition> stages)
    {
        var enemyDefinitions = (await questEnemyDefinitionRepository.GetAllAsync())
            .ToDictionary(x => x.Id);

        return stages
            .Select(stage => MapQuestStageSummary(stage, enemyDefinitions))
            .ToArray();
    }

    public async Task<object> MapQuestRoomSummaryAsync(QuestRoom room)
    {
        var stage = await questStageRepository.GetAsync(room.StageId);
        var owner = await playerRepository.GetPlayerAsync(room.OwnerId);

        return new
        {
            roomId = room.Id.Value,
            stageId = room.StageId.Value,
            stageName = stage?.Name,
            mode = room.Mode.ToString(),
            status = room.Status.ToString(),
            ownerPlayerId = room.OwnerId.Value,
            ownerDisplayName = owner?.Name ?? room.Participants.FirstOrDefault(x => x.IsOwner)?.DisplayName,
            ownerImagePath = owner?.ImagePath,
            participantCount = room.Participants.Count(x => x.Status != ParticipantStatus.Left),
            minPartyMemberCount = stage?.MinPartyMemberCount,
            maxPartyMemberCount = stage?.MaxPartyMemberCount,
            createdAt = room.CreatedAt
        };
    }

    public async Task<object> MapQuestRoomDetailAsync(QuestRoom room)
    {
        var players = (await Task.WhenAll(
                room.Participants
                    .Where(x => x.PlayerId is not null)
                    .Select(x => x.PlayerId!.Value)
                    .Distinct()
                    .Select(playerRepository.GetPlayerAsync)))
            .Where(x => x is not null)
            .ToDictionary(x => x!.Id, x => x!);

        return new
        {
            roomId = room.Id.Value,
            ownerPlayerId = room.OwnerId.Value,
            stageId = room.StageId.Value,
            mode = room.Mode.ToString(),
            status = room.Status.ToString(),
            version = room.Version,
            closeReason = room.CloseReason?.ToString(),
            createdAt = room.CreatedAt,
            closedAt = room.ClosedAt,
            canStart = room.CanStart(),
            formation = new
            {
                occupiedPositions = room.Formation.OccupiedPositions.Select(position => new
                {
                    row = position.Row.ToString(),
                    column = position.Column.ToString()
                })
            },
            participants = room.Participants.Select(participant =>
            {
                var player = participant.PlayerId is not null && players.TryGetValue(participant.PlayerId.Value, out var foundPlayer)
                    ? foundPlayer
                    : null;

                return new
                {
                    participantId = participant.Id.Value,
                    type = participant.Type.ToString(),
                    playerId = participant.PlayerId?.Value,
                    npcTemplateId = participant.NpcTemplateId?.Value,
                    displayName = participant.DisplayName,
                    imagePath = participant.Type == ParticipantType.Npc
                        ? QuestNpcImageAssignmentPolicy.Resolve(participant.Id, participant.NpcTemplateId)
                        : player?.ImagePath,
                    level = player?.Level,
                    job = player is null
                        ? null
                        : new
                        {
                            code = player.Job.ToString(),
                            displayName = ToJobDisplayName(player.Job)
                        },
                    status = participant.Status.ToString(),
                    isOwner = participant.IsOwner,
                    position = new
                    {
                        row = participant.Position.Row.ToString(),
                        column = participant.Position.Column.ToString()
                    },
                    joinedAt = participant.JoinedAt,
                    lastSeenAt = participant.LastSeenAt,
                    leftAt = participant.LeftAt
                };
            })
        };
    }

    public async Task<object> MapQuestRunDetailAsync(QuestRun run)
    {
        var enemyDefinitions = (await questEnemyDefinitionRepository.GetAllAsync())
            .ToDictionary(x => x.Id);

        var waitingParticipantIds = run.BattleState.PartyMembers
            .Where(x => x.CanAcceptManualCommand(run.TurnState.CurrentTurnNo))
            .Select(x => x.ParticipantId)
            .Except(run.TurnState.PendingCommands.Select(x => x.ParticipantId))
            .Select(x => x.Value)
            .ToArray();

        return new
        {
            runId = run.Id.Value,
            roomId = run.RoomId.Value,
            stageId = run.StageId.Value,
            status = run.Status.ToString(),
            floor = new
            {
                currentFloorNo = run.FloorState.CurrentFloorNo,
                isBossFloor = run.FloorState.IsBossFloor
            },
            turn = new
            {
                currentTurnNo = run.TurnState.CurrentTurnNo,
                actionDeadlineAt = run.TurnState.ActionDeadlineAt,
                waitingParticipantIds
            },
            partyMembers = run.PartySnapshots.Select(snapshot =>
            {
                var state = run.BattleState.PartyMembers.First(member => member.ParticipantId == snapshot.ParticipantId);
                return new
                {
                    participantId = snapshot.ParticipantId.Value,
                    type = snapshot.Type.ToString(),
                    displayName = snapshot.DisplayName,
                    imagePath = snapshot.ImagePath,
                    position = new
                    {
                        row = snapshot.StartPosition.Row.ToString(),
                        column = snapshot.StartPosition.Column.ToString()
                    },
                    currentHp = state.CurrentHp,
                    currentMp = state.CurrentMp,
                    maxHp = snapshot.BaseStatus.MaxHp,
                    maxMp = snapshot.BaseStatus.MaxMp,
                    isDead = state.IsDead,
                    canActFromTurn = state.CanActFromTurn,
                    actionMode = state.ActionMode.ToString(),
                    manualControlRequestStatus = state.IsManualControlRequested ? "Pending" : "None",
                    activeEffects = state.Ailments.Select(x => new
                    {
                        effectType = x.Type.ToString(),
                        displayName = x.Type.ToString(),
                        remainingTurns = x.RemainingTurns,
                        stacks = (int?)null
                    }).Concat(state.Buffs.Select(x => new
                    {
                        effectType = x.Stat.ToString(),
                        displayName = x.Stat.ToString(),
                        remainingTurns = x.RemainingTurns,
                        stacks = (int?)null
                    }))
                };
            }),
            enemies = run.BattleState.Enemies.Select(enemy =>
            {
                enemyDefinitions.TryGetValue(enemy.EnemyDefinitionId, out var definition);
                return new
                {
                    enemyInstanceId = enemy.Id.Value,
                    enemyDefinitionId = enemy.EnemyDefinitionId.Value,
                    name = definition?.Name ?? enemy.EnemyDefinitionId.ToString(),
                    imagePath = definition?.ImagePath,
                    position = new
                    {
                        row = enemy.Position.Row.ToString(),
                        column = enemy.Position.Column.ToString()
                    },
                    currentHp = enemy.CurrentHp,
                    currentMp = enemy.CurrentMp,
                    maxHp = definition?.Status.MaxHp,
                    maxMp = definition?.Status.MaxMp,
                    isDead = enemy.IsDead,
                    activeEffects = enemy.Ailments.Select(x => new
                    {
                        effectType = x.Type.ToString(),
                        displayName = x.Type.ToString(),
                        remainingTurns = x.RemainingTurns,
                        stacks = (int?)null
                    }).Concat(enemy.Buffs.Select(x => new
                    {
                        effectType = x.Stat.ToString(),
                        displayName = x.Stat.ToString(),
                        remainingTurns = x.RemainingTurns,
                        stacks = (int?)null
                    }))
                };
            }),
            pendingCommands = run.TurnState.PendingCommands.Select(command => new
            {
                participantId = command.ParticipantId.Value,
                turnNo = command.TurnNo,
                actionKind = command.ActionKind.ToString(),
                moveId = command.MoveId?.Id,
                selectedTargetPosition = command.SelectedTargetPosition is null
                    ? null
                    : new
                    {
                        row = command.SelectedTargetPosition.Value.Row.ToString(),
                        column = command.SelectedTargetPosition.Value.Column.ToString()
                    },
                isAutoSubmitted = command.IsAutoSubmitted,
                submittedAt = command.SubmittedAt
            }),
            chatMessages = run.ChatMessages
                .Where(message => message.TurnNo == run.TurnState.CurrentTurnNo)
                .Select(message => new
                {
                    turnNo = message.TurnNo,
                    senderParticipantId = message.SenderParticipantId.Value,
                    displayName = message.DisplayName,
                    imagePath = message.ImagePath,
                    message = message.Message,
                    sentAt = message.SentAt
                }),
            rewards = new
            {
                exp = run.Rewards.Exp
            },
            lastTurnResults = run.LastTurnResults is null
                ? null
                : new
                {
                    turnNo = run.LastTurnResults.TurnNo,
                    resolvedAt = run.LastTurnResults.ResolvedAt,
                    chatMessages = run.LastTurnResults.ChatMessages.Select(message => new
                    {
                        turnNo = message.TurnNo,
                        senderParticipantId = message.SenderParticipantId.Value,
                        displayName = message.DisplayName,
                        imagePath = message.ImagePath,
                        message = message.Message,
                        sentAt = message.SentAt
                    }),
                    actions = run.LastTurnResults.Actions.Select(action => new
                    {
                        actorParticipantId = action.ActorParticipantId,
                        actorEnemyInstanceId = action.ActorEnemyInstanceId,
                        actorDisplayName = action.ActorDisplayName,
                        actionKind = action.ActionKind,
                        moveId = action.MoveId,
                        moveName = action.MoveName,
                        succeeded = action.Succeeded,
                        targetSummaries = action.TargetSummaries.Select(target => new
                        {
                            targetParticipantId = target.TargetParticipantId,
                            targetEnemyInstanceId = target.TargetEnemyInstanceId,
                            targetDisplayName = target.TargetDisplayName,
                            resultType = target.ResultType,
                            hpChange = target.HpChange,
                            mpChange = target.MpChange,
                            appliedEffects = target.AppliedEffects,
                            removedEffects = target.RemovedEffects,
                            isDeadAfterAction = target.IsDeadAfterAction
                        }),
                        logs = action.Logs
                    }),
                    floorTransition = run.LastTurnResults.FloorTransition is null
                        ? null
                        : new
                        {
                            previousFloorNo = run.LastTurnResults.FloorTransition.PreviousFloorNo,
                            currentFloorNo = run.LastTurnResults.FloorTransition.CurrentFloorNo,
                            floorCleared = run.LastTurnResults.FloorTransition.FloorCleared,
                            bossFloorReached = run.LastTurnResults.FloorTransition.BossFloorReached
                        },
                    runTransition = run.LastTurnResults.RunTransition is null
                        ? null
                        : new
                        {
                            previousStatus = run.LastTurnResults.RunTransition.PreviousStatus,
                            currentStatus = run.LastTurnResults.RunTransition.CurrentStatus,
                            questEnded = run.LastTurnResults.RunTransition.QuestEnded
                        }
                }
        };
    }

    private static string ToJobDisplayName(Job job) =>
        job switch
        {
            Job.Apprentice => "見習い",
            Job.Warrior => "戦士",
            Job.Guardian => "盾使い",
            Job.Mage => "魔法使い",
            Job.Priest => "僧侶",
            Job.Ranger => "レンジャー",
            _ => job.ToString()
        };

    private static object MapQuestStageSummary(
        QuestStageDefinition stage,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions)
    {
        var previewEnemyImagePath = stage.Floors
            .OrderBy(floor => floor.FloorNo)
            .SelectMany(floor => floor.Placements.OrderBy(placement => placement.PlacementNo))
            .Select(placement => enemyDefinitions.TryGetValue(placement.EnemyDefinitionId, out var definition) ? definition.ImagePath : null)
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

        return new
        {
            stageId = stage.Id.Value,
            stageCode = stage.StageCode,
            name = stage.Name,
            recommendedLevel = stage.RecommendedLevel,
            previewEnemyImagePath,
            minPartyMemberCount = stage.MinPartyMemberCount,
            maxPartyMemberCount = stage.MaxPartyMemberCount,
            isActive = stage.IsActive,
            floors = stage.Floors.Select(floor => new
            {
                floorNo = floor.FloorNo,
                floorType = floor.FloorType.ToString(),
                enemyCount = floor.Placements.Count
            })
        };
    }
}
