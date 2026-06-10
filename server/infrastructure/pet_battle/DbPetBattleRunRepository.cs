using Microsoft.EntityFrameworkCore;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.pet_battle;
using server.domain.pet_battle.enums;
using server.domain.player;
using server.domain.quest.enums;

namespace server.infrastructure.pet_battle;

public class DbPetBattleRunRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IPetBattleRunRepository
{
    public async Task<PetBattleRun?> GetAsync(PetBattleRunId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PetBattleRuns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id.Value);
        if (entity is null) return null;

        var commands = await dbContext.PetBattleTurnCommands
            .AsNoTracking()
            .Where(x => x.RunId == id.Value && x.TurnNo == entity.CurrentTurnNo)
            .ToListAsync();

        return MapToDomain(entity, commands);
    }

    public async Task<PetBattleRun?> GetByRoomIdAsync(PetBattleRoomId roomId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PetBattleRuns.AsNoTracking().SingleOrDefaultAsync(x => x.RoomId == roomId.Value);
        if (entity is null) return null;

        var commands = await dbContext.PetBattleTurnCommands
            .AsNoTracking()
            .Where(x => x.RunId == entity.Id && x.TurnNo == entity.CurrentTurnNo)
            .ToListAsync();

        return MapToDomain(entity, commands);
    }

    public async Task<PetBattleRun?> GetActiveByOwnerAsync(PlayerId ownerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PetBattleRuns
            .AsNoTracking()
            .Where(x => x.OwnerPlayerId == ownerId.Value && x.Status == (int)PetBattleRunStatus.InProgress)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync();
        if (entity is null) return null;

        var commands = await dbContext.PetBattleTurnCommands
            .AsNoTracking()
            .Where(x => x.RunId == entity.Id && x.TurnNo == entity.CurrentTurnNo)
            .ToListAsync();

        return MapToDomain(entity, commands);
    }

    public async Task<IReadOnlyList<PetBattleRun>> ListExpiredAsync(DateTimeOffset now)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var entities = await dbContext.PetBattleRuns
            .AsNoTracking()
            .Where(x => x.Status == (int)PetBattleRunStatus.InProgress && x.ActionDeadlineAt <= now)
            .ToListAsync();

        if (entities.Count == 0)
        {
            return [];
        }

        var runIds = entities.Select(x => x.Id).ToList();
        var commandsByRunId = await dbContext.PetBattleTurnCommands
            .AsNoTracking()
            .Where(x => runIds.Contains(x.RunId))
            .GroupBy(x => x.RunId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList());

        return entities
            .Select(entity =>
            {
                var commands = commandsByRunId.TryGetValue(entity.Id, out var cmds)
                    ? cmds.Where(c => c.TurnNo == entity.CurrentTurnNo).ToList()
                    : [];
                return MapToDomain(entity, commands);
            })
            .ToArray();
    }

    public async Task SaveAsync(PetBattleRun run)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var exists = await dbContext.PetBattleRuns
            .AsNoTracking()
            .AnyAsync(x => x.Id == run.Id.Value);

        if (!exists)
        {
            dbContext.PetBattleRuns.Add(MapToEntity(run));
        }
        else
        {
            var entity = new PetBattleRunEntity
            {
                Id = run.Id.Value,
                RoomId = run.RoomId.Value,
                OwnerPlayerId = run.OwnerPlayerId.Value,
                OpponentPlayerId = run.OpponentPlayerId.Value,
                Status = (int)run.Status,
                CurrentTurnNo = run.TurnState.CurrentTurnNo,
                ActionDeadlineAt = run.TurnState.ActionDeadlineAt,
                LastResolvedTurnNo = run.TurnState.LastResolvedTurnNo,
                OwnerSnapshotsJson = PetBattleJsonSerializer.SerializeSnapshots(run.OwnerSnapshots),
                OpponentSnapshotsJson = PetBattleJsonSerializer.SerializeSnapshots(run.OpponentSnapshots),
                OwnerMembersJson = PetBattleJsonSerializer.SerializeMembers(run.OwnerMemberStates),
                OpponentMembersJson = PetBattleJsonSerializer.SerializeMembers(run.OpponentMemberStates),
                LastTurnResultsJson = PetBattleJsonSerializer.SerializeLastTurnResults(run.LastTurnResults),
                WinnerPlayerId = run.WinnerPlayerId?.Value,
                StartedAt = run.StartedAt,
                EndedAt = run.EndedAt,
                Version = run.Version,
            };

            dbContext.PetBattleRuns.Attach(entity);
            var entry = dbContext.Entry(entity);
            entry.Property(x => x.Version).OriginalValue = run.PersistedVersion;
            entry.Property(x => x.Status).IsModified = true;
            entry.Property(x => x.CurrentTurnNo).IsModified = true;
            entry.Property(x => x.ActionDeadlineAt).IsModified = true;
            entry.Property(x => x.LastResolvedTurnNo).IsModified = true;
            entry.Property(x => x.OwnerMembersJson).IsModified = true;
            entry.Property(x => x.OpponentMembersJson).IsModified = true;
            entry.Property(x => x.LastTurnResultsJson).IsModified = true;
            entry.Property(x => x.WinnerPlayerId).IsModified = true;
            entry.Property(x => x.EndedAt).IsModified = true;
            entry.Property(x => x.Version).IsModified = true;
        }

        await SyncTurnCommandsAsync(dbContext, run);

        await dbContext.SaveChangesAsync();
        run.SyncVersion(run.Version);
    }

    private static async Task SyncTurnCommandsAsync(AppDbContext dbContext, PetBattleRun run)
    {
        var runId = run.Id.Value;
        var currentTurnNo = run.TurnState.CurrentTurnNo;

        var resolvedCommands = await dbContext.PetBattleTurnCommands
            .Where(x => x.RunId == runId && x.TurnNo < currentTurnNo)
            .ToListAsync();
        if (resolvedCommands.Count > 0)
        {
            dbContext.PetBattleTurnCommands.RemoveRange(resolvedCommands);
        }

        var existingCommands = await dbContext.PetBattleTurnCommands
            .Where(x => x.RunId == runId && x.TurnNo == currentTurnNo)
            .ToDictionaryAsync(x => x.ParticipantId);
        var pendingByParticipantId = run.TurnState.PendingCommands.ToDictionary(x => x.ParticipantId.Value);

        foreach (var staleCommand in existingCommands.Values.Where(x => !pendingByParticipantId.ContainsKey(x.ParticipantId)))
        {
            dbContext.PetBattleTurnCommands.Remove(staleCommand);
        }

        foreach (var command in run.TurnState.PendingCommands)
        {
            if (existingCommands.TryGetValue(command.ParticipantId.Value, out var existingCommand))
            {
                existingCommand.ActionKind = (int)command.ActionKind;
                existingCommand.MoveId = command.MoveId?.Id;
                existingCommand.TargetRow = command.SelectedTargetPosition is null ? null : (int)command.SelectedTargetPosition.Value.Row;
                existingCommand.TargetColumn = command.SelectedTargetPosition is null ? null : (int)command.SelectedTargetPosition.Value.Column;
                existingCommand.SubmittedAt = command.SubmittedAt;
                existingCommand.IsAutoSubmitted = command.IsAutoSubmitted;
                continue;
            }

            dbContext.PetBattleTurnCommands.Add(new PetBattleTurnCommandEntity
            {
                RunId = runId,
                TurnNo = command.TurnNo,
                ParticipantId = command.ParticipantId.Value,
                ActionKind = (int)command.ActionKind,
                MoveId = command.MoveId?.Id,
                TargetRow = command.SelectedTargetPosition is null ? null : (int)command.SelectedTargetPosition.Value.Row,
                TargetColumn = command.SelectedTargetPosition is null ? null : (int)command.SelectedTargetPosition.Value.Column,
                SubmittedAt = command.SubmittedAt,
                IsAutoSubmitted = command.IsAutoSubmitted,
            });
        }
    }

    private static PetBattleRunEntity MapToEntity(PetBattleRun run) => new()
    {
        Id = run.Id.Value,
        RoomId = run.RoomId.Value,
        OwnerPlayerId = run.OwnerPlayerId.Value,
        OpponentPlayerId = run.OpponentPlayerId.Value,
        Status = (int)run.Status,
        CurrentTurnNo = run.TurnState.CurrentTurnNo,
        ActionDeadlineAt = run.TurnState.ActionDeadlineAt,
        LastResolvedTurnNo = run.TurnState.LastResolvedTurnNo,
        OwnerSnapshotsJson = PetBattleJsonSerializer.SerializeSnapshots(run.OwnerSnapshots),
        OpponentSnapshotsJson = PetBattleJsonSerializer.SerializeSnapshots(run.OpponentSnapshots),
        OwnerMembersJson = PetBattleJsonSerializer.SerializeMembers(run.OwnerMemberStates),
        OpponentMembersJson = PetBattleJsonSerializer.SerializeMembers(run.OpponentMemberStates),
        LastTurnResultsJson = PetBattleJsonSerializer.SerializeLastTurnResults(run.LastTurnResults),
        WinnerPlayerId = run.WinnerPlayerId?.Value,
        StartedAt = run.StartedAt,
        EndedAt = run.EndedAt,
        Version = run.Version,
    };

    private static PetBattleRun MapToDomain(PetBattleRunEntity entity, IEnumerable<PetBattleTurnCommandEntity> commandEntities)
    {
        var ownerSnapshots = PetBattleJsonSerializer.DeserializeSnapshots(entity.OwnerSnapshotsJson);
        var opponentSnapshots = PetBattleJsonSerializer.DeserializeSnapshots(entity.OpponentSnapshotsJson);
        var ownerMembers = PetBattleJsonSerializer.DeserializeMembers(entity.OwnerMembersJson);
        var opponentMembers = PetBattleJsonSerializer.DeserializeMembers(entity.OpponentMembersJson);
        var lastTurnResults = PetBattleJsonSerializer.DeserializeLastTurnResults(entity.LastTurnResultsJson);
        var commands = commandEntities.Select(c => new PetBattleSubmittedCommand(
            new PetBattleParticipantId(c.ParticipantId),
            c.TurnNo,
            (ActionKind)c.ActionKind,
            c.SubmittedAt,
            c.MoveId is null ? null : new MoveId(c.MoveId.Value),
            c.TargetRow is null ? null : new BattlePosition((BattleRow)c.TargetRow.Value, (BattleColumn)c.TargetColumn!.Value),
            c.IsAutoSubmitted)).ToArray();

        return new PetBattleRun(
            new PetBattleRunId(entity.Id),
            new PetBattleRoomId(entity.RoomId),
            new PlayerId(entity.OwnerPlayerId),
            new PlayerId(entity.OpponentPlayerId),
            ownerSnapshots,
            opponentSnapshots,
            new PetBattleTurnState(entity.CurrentTurnNo, entity.ActionDeadlineAt, commands, entity.LastResolvedTurnNo),
            ownerMembers,
            opponentMembers,
            lastTurnResults,
            entity.StartedAt,
            (PetBattleRunStatus)entity.Status,
            entity.EndedAt,
            entity.WinnerPlayerId is null ? null : new PlayerId(entity.WinnerPlayerId.Value),
            entity.Version);
    }
}
