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
        var runIds = await dbContext.PetBattleRuns
            .AsNoTracking()
            .Where(x => x.Status == (int)PetBattleRunStatus.InProgress && x.ActionDeadlineAt <= now)
            .Select(x => x.Id)
            .ToListAsync();

        var runs = new List<PetBattleRun>(runIds.Count);
        foreach (var runId in runIds)
        {
            var run = await GetAsync(new PetBattleRunId(runId));
            if (run is not null) runs.Add(run);
        }

        return runs;
    }

    public async Task SaveAsync(PetBattleRun run)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.PetBattleRuns.SingleOrDefaultAsync(x => x.Id == run.Id.Value);

        if (existing is null)
        {
            dbContext.PetBattleRuns.Add(MapToEntity(run));
        }
        else
        {
            existing.Status = (int)run.Status;
            existing.CurrentTurnNo = run.TurnState.CurrentTurnNo;
            existing.ActionDeadlineAt = run.TurnState.ActionDeadlineAt;
            existing.LastResolvedTurnNo = run.TurnState.LastResolvedTurnNo;
            existing.OwnerMembersJson = PetBattleJsonSerializer.SerializeMembers(run.OwnerMemberStates);
            existing.OpponentMembersJson = PetBattleJsonSerializer.SerializeMembers(run.OpponentMemberStates);
            existing.LastTurnResultsJson = PetBattleJsonSerializer.SerializeLastTurnResults(run.LastTurnResults);
            existing.WinnerPlayerId = run.WinnerPlayerId?.Value;
            existing.EndedAt = run.EndedAt;
        }

        // Replace current-turn commands
        var runId = run.Id.Value;
        var oldCommands = await dbContext.PetBattleTurnCommands
            .Where(x => x.RunId == runId && x.TurnNo == run.TurnState.CurrentTurnNo)
            .ToListAsync();
        dbContext.PetBattleTurnCommands.RemoveRange(oldCommands);
        dbContext.PetBattleTurnCommands.AddRange(run.TurnState.PendingCommands.Select(c => new PetBattleTurnCommandEntity
        {
            RunId = runId,
            TurnNo = c.TurnNo,
            ParticipantId = c.ParticipantId.Value,
            ActionKind = (int)c.ActionKind,
            MoveId = c.MoveId?.Id,
            TargetRow = c.SelectedTargetPosition is null ? null : (int)c.SelectedTargetPosition.Value.Row,
            TargetColumn = c.SelectedTargetPosition is null ? null : (int)c.SelectedTargetPosition.Value.Column,
            SubmittedAt = c.SubmittedAt,
            IsAutoSubmitted = c.IsAutoSubmitted,
        }));

        await dbContext.SaveChangesAsync();
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
            entity.WinnerPlayerId is null ? null : new PlayerId(entity.WinnerPlayerId.Value));
    }
}
