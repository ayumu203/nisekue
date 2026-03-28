using Microsoft.EntityFrameworkCore;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.infrastructure.quest.run;

public class DbQuestRunRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IQuestRunRepository
{
    public Task<QuestRun?> GetAsync(QuestRunId id)
    {
        return LoadAsync(x => x.Id == id.Value);
    }

    public Task<QuestRun?> GetByRoomIdAsync(QuestRoomId roomId)
    {
        return LoadAsync(x => x.RoomId == roomId.Value);
    }

    public async Task<QuestRun?> GetActiveByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var runId = await (
            from run in dbContext.QuestRuns.AsNoTracking()
            join participant in dbContext.QuestRoomParticipants.AsNoTracking()
                on run.RoomId equals participant.RoomId
            where run.Status == (int)QuestRunStatus.InProgress
                && participant.PlayerId == playerId.Value
                && participant.ParticipantType == (int)ParticipantType.Player
                && participant.ParticipantStatus != (int)ParticipantStatus.Left
            orderby run.StartedAt descending
            select run.Id)
            .FirstOrDefaultAsync();

        return runId == Guid.Empty ? null : await GetAsync(new QuestRunId(runId));
    }

    public async Task<bool> ExistsActiveRunByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await (
            from run in dbContext.QuestRuns.AsNoTracking()
            join participant in dbContext.QuestRoomParticipants.AsNoTracking()
                on run.RoomId equals participant.RoomId
            where run.Status == (int)QuestRunStatus.InProgress
                && participant.PlayerId == playerId.Value
                && participant.ParticipantType == (int)ParticipantType.Player
                && participant.ParticipantStatus != (int)ParticipantStatus.Left
            select run.Id)
            .AnyAsync();
    }

    public async Task<IReadOnlyList<QuestRun>> ListExpiredAsync(DateTimeOffset now)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var runIds = await dbContext.QuestRuns
            .AsNoTracking()
            .Where(x => x.Status == (int)QuestRunStatus.InProgress && x.ActionDeadlineAt <= now)
            .Select(x => x.Id)
            .ToListAsync();

        var runs = new List<QuestRun>(runIds.Count);
        foreach (var runId in runIds)
        {
            var run = await GetAsync(new QuestRunId(runId));
            if (run is not null)
            {
                runs.Add(run);
            }
        }

        return runs;
    }

    public async Task SaveAsync(QuestRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.QuestRuns.SingleOrDefaultAsync(x => x.Id == run.Id.Value);
        if (existing is null)
        {
            dbContext.QuestRuns.Add(new QuestRunEntity
            {
                Id = run.Id.Value,
                RoomId = run.RoomId.Value,
                StageId = run.StageId.Value,
                Status = (int)run.Status,
                CurrentFloorNo = run.FloorState.CurrentFloorNo,
                CurrentTurnNo = run.TurnState.CurrentTurnNo,
                ActionDeadlineAt = run.TurnState.ActionDeadlineAt,
                LastResolvedTurnNo = run.TurnState.LastResolvedTurnNo,
                LastTurnResultsJson = QuestJsonSerializer.SerializeLastTurnResults(run.LastTurnResults),
                ChatMessagesJson = QuestJsonSerializer.SerializeChatMessages(run.ChatMessages),
                StartedAt = run.StartedAt,
                EndedAt = run.EndedAt
            });
        }
        else
        {
            existing.StageId = run.StageId.Value;
            existing.Status = (int)run.Status;
            existing.CurrentFloorNo = run.FloorState.CurrentFloorNo;
            existing.CurrentTurnNo = run.TurnState.CurrentTurnNo;
            existing.ActionDeadlineAt = run.TurnState.ActionDeadlineAt;
            existing.LastResolvedTurnNo = run.TurnState.LastResolvedTurnNo;
            existing.LastTurnResultsJson = QuestJsonSerializer.SerializeLastTurnResults(run.LastTurnResults);
            existing.ChatMessagesJson = QuestJsonSerializer.SerializeChatMessages(run.ChatMessages);
            existing.EndedAt = run.EndedAt;
        }

        await ReplaceChildrenAsync(dbContext, run);
        await dbContext.SaveChangesAsync();
    }

    private async Task<QuestRun?> LoadAsync(Func<QuestRunEntity, bool> predicate)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var runEntity = dbContext.QuestRuns
            .AsNoTracking()
            .AsEnumerable()
            .SingleOrDefault(predicate);
        if (runEntity is null)
        {
            return null;
        }

        var partyMemberEntities = await dbContext.QuestRunPartyMembers
            .AsNoTracking()
            .Where(x => x.RunId == runEntity.Id)
            .ToListAsync();
        var snapshotEntities = await dbContext.QuestRunPartySnapshots
            .AsNoTracking()
            .Where(x => x.RunId == runEntity.Id)
            .ToListAsync();
        var enemyEntities = await dbContext.QuestRunEnemies
            .AsNoTracking()
            .Where(x => x.RunId == runEntity.Id)
            .ToListAsync();
        var turnCommandEntities = await dbContext.QuestTurnCommands
            .AsNoTracking()
            .Where(x => x.RunId == runEntity.Id && x.TurnNo == runEntity.CurrentTurnNo)
            .ToListAsync();
        var trapEntities = await dbContext.QuestFloorTraps
            .AsNoTracking()
            .Where(x => x.RunId == runEntity.Id)
            .ToListAsync();
        var rewardEntity = await dbContext.QuestRewardSummaries
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.RunId == runEntity.Id);

        var snapshots = snapshotEntities.Select(MapSnapshot).ToArray();
        var partyMembers = partyMemberEntities.Select(MapPartyMember).ToArray();
        var enemies = enemyEntities.Select(MapEnemy).ToArray();
        var turnCommands = turnCommandEntities.Select(MapTurnCommand).ToArray();
        var traps = trapEntities.Select(MapTrap).ToArray();
        var lastTurnResults = QuestJsonSerializer.DeserializeLastTurnResults(runEntity.LastTurnResultsJson);
        var chatMessages = QuestJsonSerializer.DeserializeChatMessages(runEntity.ChatMessagesJson);

        return new QuestRun(
            new QuestRunId(runEntity.Id),
            new QuestRoomId(runEntity.RoomId),
            new QuestStageId(runEntity.StageId),
            snapshots,
            new QuestFloorState(runEntity.CurrentFloorNo, false, []),
            new QuestBattleState(partyMembers, enemies),
            new QuestTurnState(runEntity.CurrentTurnNo, runEntity.ActionDeadlineAt, turnCommands, runEntity.LastResolvedTurnNo),
            new QuestTrapCollection(traps),
            new QuestRewardAccumulator(rewardEntity?.Exp ?? 0),
            lastTurnResults,
            chatMessages,
            runEntity.StartedAt,
            (QuestRunStatus)runEntity.Status,
            runEntity.EndedAt);
    }

    private static async Task ReplaceChildrenAsync(AppDbContext dbContext, QuestRun run)
    {
        var runId = run.Id.Value;

        dbContext.QuestRunPartyMembers.RemoveRange(await dbContext.QuestRunPartyMembers.Where(x => x.RunId == runId).ToListAsync());
        dbContext.QuestRunPartySnapshots.RemoveRange(await dbContext.QuestRunPartySnapshots.Where(x => x.RunId == runId).ToListAsync());
        dbContext.QuestRunEnemies.RemoveRange(await dbContext.QuestRunEnemies.Where(x => x.RunId == runId).ToListAsync());
        dbContext.QuestTurnCommands.RemoveRange(await dbContext.QuestTurnCommands.Where(x => x.RunId == runId).ToListAsync());
        dbContext.QuestFloorTraps.RemoveRange(await dbContext.QuestFloorTraps.Where(x => x.RunId == runId).ToListAsync());

        var reward = await dbContext.QuestRewardSummaries.SingleOrDefaultAsync(x => x.RunId == runId);
        if (reward is null)
        {
            dbContext.QuestRewardSummaries.Add(new QuestRewardSummaryEntity
            {
                RunId = runId,
                Exp = run.Rewards.Exp
            });
        }
        else
        {
            reward.Exp = run.Rewards.Exp;
        }

        dbContext.QuestRunPartySnapshots.AddRange(run.PartySnapshots.Select(x => new QuestRunPartySnapshotEntity
        {
            RunId = runId,
            ParticipantId = x.ParticipantId.Value,
            ParticipantType = (int)x.Type,
            DisplayName = x.DisplayName,
            ImagePath = x.ImagePath,
            Job = (int)x.Job,
            WeaponPlayerEquipmentId = x.WeaponEquipmentId?.Value,
            ArmorPlayerEquipmentId = x.ArmorEquipmentId?.Value,
            StartRow = (int)x.StartPosition.Row,
            StartColumn = (int)x.StartPosition.Column,
            MaxHp = x.BaseStatus.MaxHp,
            MaxMp = x.BaseStatus.MaxMp,
            Strength = x.BaseStatus.Strength,
            Defense = x.BaseStatus.Defense,
            Intelligence = x.BaseStatus.Intelligence,
            Luck = x.BaseStatus.Luck,
            Speed = x.BaseStatus.Speed,
            MoveSetJson = QuestJsonSerializer.SerializeMoveSet(x.MoveSet),
            InitialActionMode = (int)x.InitialActionMode
        }));

        dbContext.QuestRunPartyMembers.AddRange(run.BattleState.PartyMembers.Select(x =>
        {
            var effectsJson = QuestJsonSerializer.SerializeEffects(x.Ailments, x.Buffs);
            return new QuestRunPartyMemberEntity
            {
                RunId = runId,
                ParticipantId = x.ParticipantId.Value,
                CurrentHp = x.CurrentHp,
                CurrentMp = x.CurrentMp,
                IsDead = x.IsDead,
                CanActFromTurn = x.CanActFromTurn,
                ActionMode = (int)x.ActionMode,
                HasLeftQuest = x.HasLeftQuest,
                IsManualControlRequested = x.IsManualControlRequested,
                ActiveEffectsJson = effectsJson,
                DerivedParametersJson = QuestJsonSerializer.SerializeDerivedParametersPlaceholder(),
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }));

        dbContext.QuestRunEnemies.AddRange(run.BattleState.Enemies.Select(x =>
        {
            var effectsJson = QuestJsonSerializer.SerializeEffects(x.Ailments, x.Buffs);
            return new QuestRunEnemyEntity
            {
                RunId = runId,
                EnemyInstanceId = x.Id.Value,
                FloorNo = run.FloorState.CurrentFloorNo,
                EnemyDefinitionId = x.EnemyDefinitionId.Value,
                BattleRow = (int)x.Position.Row,
                BattleColumn = (int)x.Position.Column,
                CurrentHp = x.CurrentHp,
                CurrentMp = x.CurrentMp,
                IsDead = x.IsDead,
                ActiveEffectsJson = effectsJson,
                DerivedParametersJson = QuestJsonSerializer.SerializeDerivedParametersPlaceholder()
            };
        }));

        dbContext.QuestTurnCommands.AddRange(run.TurnState.PendingCommands.Select(x => new QuestTurnCommandEntity
        {
            RunId = runId,
            TurnNo = x.TurnNo,
            ParticipantId = x.ParticipantId.Value,
            ActionKind = (int)x.ActionKind,
            MoveId = x.MoveId?.Id,
            TargetRow = x.SelectedTargetPosition is null ? null : (int)x.SelectedTargetPosition.Value.Row,
            TargetColumn = x.SelectedTargetPosition is null ? null : (int)x.SelectedTargetPosition.Value.Column,
            SubmittedAt = x.SubmittedAt,
            IsAutoSubmitted = x.IsAutoSubmitted
        }));

        dbContext.QuestFloorTraps.AddRange(run.Traps.Traps.Select(x => new QuestFloorTrapEntity
        {
            RunId = runId,
            TrapId = x.Id.Value,
            SourceParticipantId = x.SourceParticipantId.Value,
            MoveId = x.MoveId.Id,
            ExpiresAfterFloorNo = x.ExpiresAfterFloorNo,
            IsTriggered = x.IsTriggered
        }));
    }

    private static QuestRunPartyMemberState MapPartyMember(QuestRunPartyMemberEntity entity)
    {
        var (ailments, buffs) = QuestJsonSerializer.DeserializeEffects(entity.ActiveEffectsJson);
        return new QuestRunPartyMemberState(
            new QuestParticipantId(entity.ParticipantId),
            entity.CurrentHp,
            entity.CurrentMp,
            entity.IsDead,
            entity.CanActFromTurn,
            (ActionMode)entity.ActionMode,
            entity.HasLeftQuest,
            entity.IsManualControlRequested,
            ailments,
            buffs);
    }

    private static QuestRunPartyMemberSnapshot MapSnapshot(QuestRunPartySnapshotEntity entity)
    {
        return new QuestRunPartyMemberSnapshot(
            new QuestParticipantId(entity.ParticipantId),
            (ParticipantType)entity.ParticipantType,
            entity.DisplayName,
            entity.ImagePath,
            (Job)entity.Job,
            new Status(
                entity.MaxHp,
                entity.MaxMp,
                entity.Strength,
                entity.Defense,
                entity.Intelligence,
                entity.Luck,
                entity.Speed),
            entity.WeaponPlayerEquipmentId is null ? null : new PlayerEquipmentId(entity.WeaponPlayerEquipmentId.Value),
            entity.ArmorPlayerEquipmentId is null ? null : new PlayerEquipmentId(entity.ArmorPlayerEquipmentId.Value),
            QuestJsonSerializer.DeserializeMoveSet(entity.MoveSetJson),
            new BattlePosition((BattleRow)entity.StartRow, (BattleColumn)entity.StartColumn),
            (ActionMode)entity.InitialActionMode);
    }

    private static QuestEnemyState MapEnemy(QuestRunEnemyEntity entity)
    {
        var (ailments, buffs) = QuestJsonSerializer.DeserializeEffects(entity.ActiveEffectsJson);
        return new QuestEnemyState(
            new QuestEnemyInstanceId(entity.EnemyInstanceId),
            new QuestEnemyDefinitionId(entity.EnemyDefinitionId),
            new BattlePosition((BattleRow)entity.BattleRow, (BattleColumn)entity.BattleColumn),
            entity.CurrentHp,
            entity.CurrentMp,
            entity.IsDead,
            ailments,
            buffs);
    }

    private static QuestSubmittedCommand MapTurnCommand(QuestTurnCommandEntity entity)
    {
        BattlePosition? targetPosition = null;
        if (entity.TargetRow is not null && entity.TargetColumn is not null)
        {
            targetPosition = new BattlePosition((BattleRow)entity.TargetRow.Value, (BattleColumn)entity.TargetColumn.Value);
        }

        return new QuestSubmittedCommand(
            new QuestParticipantId(entity.ParticipantId),
            entity.TurnNo,
            (ActionKind)entity.ActionKind,
            entity.SubmittedAt,
            entity.MoveId is null ? null : new MoveId(entity.MoveId.Value),
            targetPosition,
            entity.IsAutoSubmitted);
    }

    private static QuestTrapState MapTrap(QuestFloorTrapEntity entity)
    {
        return new QuestTrapState(
            new QuestTrapId(entity.TrapId),
            new QuestParticipantId(entity.SourceParticipantId),
            new MoveId(entity.MoveId),
            entity.ExpiresAfterFloorNo,
            entity.IsTriggered);
    }
}
