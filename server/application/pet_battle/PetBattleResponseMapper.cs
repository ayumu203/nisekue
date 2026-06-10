using server.domain.move;
using server.domain.pet_battle;
using server.domain.player;

namespace server.application.pet_battle;

public class PetBattleResponseMapper(IMoveRepository moveRepository, IPlayerRepository playerRepository)
{
    public async Task<object> MapRoomAsync(PetBattleRoom room)
    {
        var playerNames = await GetPlayerNamesAsync(room.OwnerPlayerId, room.OpponentPlayerId);

        return new
        {
            roomId = room.Id.Value,
            status = room.Status.ToString(),
            ownerPlayerId = room.OwnerPlayerId.Value,
            ownerPlayerName = playerNames.GetValueOrDefault(room.OwnerPlayerId),
            opponentPlayerId = room.OpponentPlayerId.Value,
            opponentPlayerName = playerNames.GetValueOrDefault(room.OpponentPlayerId),
            slots = room.Slots.Select(s => new
            {
                petId = s.PetId.Value,
                row = s.Row.ToString(),
                column = s.Column.ToString()
            }).ToArray(),
            createdAt = room.CreatedAt
        };
    }

    public async Task<object> MapRunAsync(PetBattleRun run)
    {
        var playerNames = await GetPlayerNamesAsync(run.OwnerPlayerId, run.OpponentPlayerId);
        var movesById = (await moveRepository.GetAllMovesAsync()).ToDictionary(m => m.Id.Id);

        var waitingParticipantIds = run.OwnerMemberStates
            .Where(m => m.CanAcceptManualCommand(run.TurnState.CurrentTurnNo))
            .Select(m => m.ParticipantId)
            .Except(run.TurnState.PendingCommands.Select(c => c.ParticipantId))
            .Select(id => id.Value)
            .ToArray();

        return new
        {
            runId = run.Id.Value,
            roomId = run.RoomId.Value,
            status = run.Status.ToString(),
            ownerPlayerId = run.OwnerPlayerId.Value,
            ownerPlayerName = playerNames.GetValueOrDefault(run.OwnerPlayerId),
            opponentPlayerId = run.OpponentPlayerId.Value,
            opponentPlayerName = playerNames.GetValueOrDefault(run.OpponentPlayerId),
            winnerPlayerId = run.WinnerPlayerId?.Value,
            currentTurnNo = run.TurnState.CurrentTurnNo,
            actionDeadlineAt = run.TurnState.ActionDeadlineAt,
            startedAt = run.StartedAt,
            endedAt = run.EndedAt,
            waitingParticipantIds,
            submittedParticipantIds = run.TurnState.PendingCommands.Select(c => c.ParticipantId.Value).ToArray(),
            ownerMembers = run.OwnerSnapshots.Select(s => MapMember(s, run.OwnerMemberStates, movesById)).ToArray(),
            opponentMembers = run.OpponentSnapshots.Select(s => MapMember(s, run.OpponentMemberStates, movesById)).ToArray(),
            lastTurnResults = run.LastTurnResults is null ? null : MapLastTurnResults(run.LastTurnResults)
        };
    }

    private static object MapMember(
        PetBattlePartyMemberSnapshot snapshot,
        IReadOnlyList<PetBattlePartyMemberState> memberStates,
        IReadOnlyDictionary<int, Move> movesById)
    {
        var state = memberStates.FirstOrDefault(m => m.ParticipantId == snapshot.ParticipantId);
        return new
        {
            participantId = snapshot.ParticipantId.Value,
            enemyDefinitionId = snapshot.EnemyDefinitionId,
            displayName = snapshot.DisplayName,
            imagePath = snapshot.ImagePath,
            maxHp = snapshot.BaseStatus.MaxHp,
            maxMp = snapshot.BaseStatus.MaxMp,
            currentHp = state?.CurrentHp ?? snapshot.BaseStatus.MaxHp,
            currentMp = state?.CurrentMp ?? snapshot.BaseStatus.MaxMp,
            isDead = state?.IsDead ?? false,
            moves = snapshot.MoveIds
                .Where(movesById.ContainsKey)
                .Select(moveId =>
                {
                    var move = movesById[moveId];
                    return new
                    {
                        moveId = move.Id.Id,
                        name = move.Name,
                        mpCost = move.MpCost,
                        targetType = move.TargetType.ToString(),
                        targetLifeState = move.TargetLifeState.ToString(),
                        attackRange = move.AttackRange.ToString()
                    };
                }).ToArray(),
            startRow = snapshot.StartRow.ToString(),
            startColumn = snapshot.StartColumn.ToString(),
            activeEffects = (state?.Ailments ?? []).Select(x => new
            {
                effectType = x.Type.ToString(),
                remainingTurns = x.RemainingTurns
            }).Concat((state?.Buffs ?? []).Select(x => new
            {
                effectType = x.Stat.ToString(),
                remainingTurns = x.RemainingTurns
            })).ToArray()
        };
    }

    private static object MapLastTurnResults(PetBattleLastTurnResults results) => new
    {
        turnNo = results.TurnNo,
        resolvedAt = results.ResolvedAt,
        actions = results.Actions.Select(a => new
        {
            actorParticipantId = a.ActorParticipantId,
            actorDisplayName = a.ActorDisplayName,
            actionKind = a.ActionKind,
            moveId = a.MoveId,
            moveName = a.MoveName,
            succeeded = a.Succeeded,
            targetSummaries = a.TargetSummaries.Select(t => new
            {
                targetParticipantId = t.TargetParticipantId,
                targetDisplayName = t.TargetDisplayName,
                resultType = t.ResultType,
                hpChange = t.HpChange,
                mpChange = t.MpChange,
                appliedEffects = t.AppliedEffects,
                isDeadAfterAction = t.IsDeadAfterAction
            }).ToArray()
        }).ToArray()
    };

    private async Task<IReadOnlyDictionary<PlayerId, string>> GetPlayerNamesAsync(params PlayerId[] playerIds)
    {
        var players = await playerRepository.GetPlayersAsync(playerIds.Distinct());
        return players.ToDictionary(p => p.Id, p => p.Name);
    }
}
