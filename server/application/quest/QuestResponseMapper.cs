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
            participantCount = room.Participants.Count(x => x.Status != ParticipantStatus.Left),
            minPartyMemberCount = stage?.MinPartyMemberCount,
            maxPartyMemberCount = stage?.MaxPartyMemberCount,
            createdAt = room.CreatedAt
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
            chatMessages = run.ChatMessages.Select(message => new
            {
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
}
