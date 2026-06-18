using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.infrastructure.quest.run;

public class DbQuestRunRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IQuestRunRepository
{
    private const int SaveChatRetryCount = 3;

    public async Task<QuestRun?> GetAsync(QuestRunId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var runEntity = await dbContext.QuestRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value);
        if (runEntity is null)
        {
            return null;
        }

        return await LoadAsync(dbContext, runEntity);
    }

    public async Task<QuestRun?> GetForResolutionAsync(QuestRunId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        // 表示専用で重い last_turn_results_json は射影から除外し、解決に必要な列のみ取得する。
        var projected = await dbContext.QuestRuns
            .AsNoTracking()
            .Where(x => x.Id == id.Value)
            .Select(LightRunProjection)
            .SingleOrDefaultAsync();
        if (projected is null)
        {
            return null;
        }

        return await LoadAsync(dbContext, ToLightRunEntity(projected));
    }

    public async Task<QuestRoomId?> GetRoomIdAsync(QuestRunId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        // 参加者検証はルームIDだけ分かればよいため、集約全体（特に last_turn_results_json）を読まない。
        var roomId = await dbContext.QuestRuns
            .AsNoTracking()
            .Where(x => x.Id == id.Value)
            .Select(x => (Guid?)x.RoomId)
            .SingleOrDefaultAsync();

        return roomId is null ? null : new QuestRoomId(roomId.Value);
    }

    public async Task<QuestRun?> GetByRoomIdAsync(QuestRoomId roomId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var runEntity = await dbContext.QuestRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.RoomId == roomId.Value);
        if (runEntity is null)
        {
            return null;
        }

        return await LoadAsync(dbContext, runEntity);
    }

    public async Task<QuestRun?> GetActiveByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var runEntity = await (
            from run in dbContext.QuestRuns.AsNoTracking()
            join participant in dbContext.QuestRoomParticipants.AsNoTracking()
                on run.RoomId equals participant.RoomId
            where run.Status == (int)QuestRunStatus.InProgress
                && participant.PlayerId == playerId.Value
                && participant.ParticipantType == (int)ParticipantType.Player
                && participant.ParticipantStatus != (int)ParticipantStatus.Left
            orderby run.StartedAt descending
            select run)
            .FirstOrDefaultAsync();
        if (runEntity is null)
        {
            return null;
        }

        return await LoadAsync(dbContext, runEntity);
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
        // 自動処理は直近ターン結果を参照しない（解決時に再生成される）ため last_turn_results_json は読まない。
        var projectedRuns = await dbContext.QuestRuns
            .AsNoTracking()
            .Where(x => x.Status == (int)QuestRunStatus.InProgress && x.ActionDeadlineAt <= now)
            .Select(LightRunProjection)
            .ToListAsync();
        if (projectedRuns.Count == 0)
        {
            return [];
        }

        var runEntities = projectedRuns.Select(ToLightRunEntity).ToList();

        var runIds = runEntities.Select(x => x.Id).ToList();
        var partyMemberByRunId = await dbContext.QuestRunPartyMembers
            .AsNoTracking()
            .Where(x => runIds.Contains(x.RunId))
            .GroupBy(x => x.RunId)
            .ToDictionaryAsync(x => x.Key, x => x.ToList());
        var snapshotsByRunId = await dbContext.QuestRunPartySnapshots
            .AsNoTracking()
            .Where(x => runIds.Contains(x.RunId))
            .GroupBy(x => x.RunId)
            .ToDictionaryAsync(x => x.Key, x => x.ToList());
        var enemiesByRunId = await dbContext.QuestRunEnemies
            .AsNoTracking()
            .Where(x => runIds.Contains(x.RunId))
            .GroupBy(x => x.RunId)
            .ToDictionaryAsync(x => x.Key, x => x.ToList());
        var currentTurnNoByRunId = runEntities.ToDictionary(x => x.Id, x => x.CurrentTurnNo);
        var currentTurnNos = runEntities.Select(x => x.CurrentTurnNo).Distinct().ToArray();
        var turnCommandsByRunId = (await dbContext.QuestTurnCommands
            .AsNoTracking()
            .Where(x => runIds.Contains(x.RunId) && currentTurnNos.Contains(x.TurnNo))
            .ToListAsync())
            .Where(x => currentTurnNoByRunId.GetValueOrDefault(x.RunId) == x.TurnNo)
            .GroupBy(x => x.RunId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var trapsByRunId = await dbContext.QuestFloorTraps
            .AsNoTracking()
            .Where(x => runIds.Contains(x.RunId))
            .GroupBy(x => x.RunId)
            .ToDictionaryAsync(x => x.Key, x => x.ToList());
        var rewardsByRunId = await dbContext.QuestRewardSummaries
            .AsNoTracking()
            .Where(x => runIds.Contains(x.RunId))
            .ToDictionaryAsync(x => x.RunId, x => x);

        return runEntities.Select(entity => MapToDomain(
                entity,
                snapshotsByRunId.GetValueOrDefault(entity.Id) ?? [],
                partyMemberByRunId.GetValueOrDefault(entity.Id) ?? [],
                enemiesByRunId.GetValueOrDefault(entity.Id) ?? [],
                turnCommandsByRunId.GetValueOrDefault(entity.Id) ?? [],
                trapsByRunId.GetValueOrDefault(entity.Id) ?? [],
                rewardsByRunId.GetValueOrDefault(entity.Id)))
            .ToArray();
    }

    public async Task SaveAsync(QuestRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var exists = await dbContext.QuestRuns
            .AsNoTracking()
            .AnyAsync(x => x.Id == run.Id.Value);

        try
        {
            if (!exists)
            {
                dbContext.QuestRuns.Add(MapToEntity(run));
                AddChildrenForCreate(dbContext, run);
                await SyncRewardSummaryAsync(dbContext, run);
            }
            else
            {
                var entity = MapToEntity(run);
                dbContext.QuestRuns.Attach(entity);
                var entry = dbContext.Entry(entity);
                entry.Property(x => x.Version).OriginalValue = run.PersistedVersion;
                entry.Property(x => x.StageId).IsModified = true;
                entry.Property(x => x.Status).IsModified = true;
                entry.Property(x => x.CurrentFloorNo).IsModified = true;
                entry.Property(x => x.CurrentTurnNo).IsModified = true;
                entry.Property(x => x.ActionDeadlineAt).IsModified = true;
                entry.Property(x => x.LastResolvedTurnNo).IsModified = true;
                // last_turn_results_json は解決でダーティになった時だけ書く。軽量ロード（未取得）由来でも
                // ダーティでなければ既存値を温存し、null での上書き事故を防ぐ。
                entry.Property(x => x.LastTurnResultsJson).IsModified = run.LastTurnResultsDirty;
                entry.Property(x => x.ChatMessagesJson).IsModified = true;
                entry.Property(x => x.EndedAt).IsModified = true;
                entry.Property(x => x.Version).IsModified = true;

                await SyncPartyMembersAsync(dbContext, run);
                await SyncEnemiesAsync(dbContext, run);
                await SyncTurnCommandsAsync(dbContext, run);
                await SyncTrapsAsync(dbContext, run);
                await SyncRewardSummaryAsync(dbContext, run);
            }

            await dbContext.SaveChangesAsync();
            run.SyncVersion(run.Version);
            run.MarkChatMessagesPersisted();
            run.MarkLastTurnResultsPersisted();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await SyncRunVersionWithLatestAsync(run);
            throw new InvalidOperationException("クエスト進行情報が同時更新されました。最新状態を再取得してからやり直してください。", ex);
        }
    }

    public async Task SaveChatMessagesAsync(QuestRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var pendingMessages = run.PendingChatMessages.ToArray();
        if (pendingMessages.Length == 0)
        {
            return;
        }

        var messagesToPersist = run.ChatMessages.ToArray();
        var nextVersion = run.Version;

        for (var attempt = 1; attempt <= SaveChatRetryCount; attempt++)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var entity = new QuestRunEntity
            {
                Id = run.Id.Value,
                ChatMessagesJson = QuestJsonSerializer.SerializeChatMessages(messagesToPersist),
                Version = nextVersion
            };
            dbContext.QuestRuns.Attach(entity);

            var entry = dbContext.Entry(entity);
            entry.Property(x => x.Version).OriginalValue = run.PersistedVersion;
            entry.Property(x => x.ChatMessagesJson).IsModified = true;
            entry.Property(x => x.Version).IsModified = true;

            try
            {
                await dbContext.SaveChangesAsync();
                run.SetChatMessagesForPersistence(messagesToPersist);
                run.SyncVersion(nextVersion);
                run.MarkChatMessagesPersisted();
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await SyncRunVersionWithLatestAsync(run);
                if (attempt == SaveChatRetryCount)
                {
                    throw new InvalidOperationException("クエスト進行情報が同時更新されました。最新状態を再取得してからやり直してください。", ex);
                }

                var latest = await LoadRunVersionAndChatMessagesAsync(run.Id);
                if (latest is null)
                {
                    throw new KeyNotFoundException($"クエスト進行情報が見つかりません。 runId={run.Id.Value}");
                }

                messagesToPersist = MergeChatMessages(latest.Value.ChatMessages, pendingMessages);
                nextVersion = latest.Value.Version + 1;
            }
        }

        throw new InvalidOperationException("チャット保存のリトライに失敗しました。");
    }

    private async Task SyncRunVersionWithLatestAsync(QuestRun run)
    {
        var latest = await LoadRunVersionAndChatMessagesAsync(run.Id);
        if (latest is null)
        {
            return;
        }

        run.SyncVersion(latest.Value.Version);
    }

    private async Task<(int Version, QuestChatMessage[] ChatMessages)?> LoadRunVersionAndChatMessagesAsync(QuestRunId runId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var latest = await dbContext.QuestRuns
            .AsNoTracking()
            .Where(x => x.Id == runId.Value)
            .Select(x => new { x.Version, x.ChatMessagesJson })
            .SingleOrDefaultAsync();
        if (latest is null)
        {
            return null;
        }

        return (latest.Version, QuestJsonSerializer.DeserializeChatMessages(latest.ChatMessagesJson));
    }

    private static QuestChatMessage[] MergeChatMessages(
        IEnumerable<QuestChatMessage> existingMessages,
        IEnumerable<QuestChatMessage> pendingMessages)
    {
        return existingMessages
            .Concat(pendingMessages)
            .DistinctBy(message => (message.SenderParticipantId.Value, message.SentAt, message.TurnNo, message.Message))
            .OrderBy(message => message.SentAt)
            .ThenBy(message => message.SenderParticipantId.Value)
            .ThenBy(message => message.Message)
            .ToArray();
    }

    private static void AddChildrenForCreate(AppDbContext dbContext, QuestRun run)
    {
        var runId = run.Id.Value;

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
            InitialActionMode = (int)x.InitialActionMode,
            PetEnemyDefinitionId = x.Pet?.EnemyDefinitionId.Value,
            PetMaxHp = x.Pet?.Status.MaxHp,
            PetMaxMp = x.Pet?.Status.MaxMp,
            PetStrength = x.Pet?.Status.Strength,
            PetDefense = x.Pet?.Status.Defense,
            PetIntelligence = x.Pet?.Status.Intelligence,
            PetLuck = x.Pet?.Status.Luck,
            PetSpeed = x.Pet?.Status.Speed
        }));

        dbContext.QuestRunPartyMembers.AddRange(run.BattleState.PartyMembers.Select(x => ToPartyMemberEntity(runId, x)));
        dbContext.QuestRunEnemies.AddRange(run.BattleState.Enemies.Select(x => ToEnemyEntity(run, x)));
        dbContext.QuestTurnCommands.AddRange(run.TurnState.PendingCommands.Select(x => ToTurnCommandEntity(runId, x)));
        dbContext.QuestFloorTraps.AddRange(run.Traps.Traps.Select(x => ToTrapEntity(runId, x)));
    }

    private static async Task SyncPartyMembersAsync(AppDbContext dbContext, QuestRun run)
    {
        var runId = run.Id.Value;
        var existingEntities = await dbContext.QuestRunPartyMembers
            .Where(x => x.RunId == runId)
            .ToDictionaryAsync(x => x.ParticipantId);

        SyncEntities(
            existingEntities,
            run.BattleState.PartyMembers,
            state => state.ParticipantId.Value,
            stale => dbContext.QuestRunPartyMembers.Remove(stale),
            state => dbContext.QuestRunPartyMembers.Add(ToPartyMemberEntity(runId, state)),
            ApplyPartyMemberEntity);
    }

    private static async Task SyncEnemiesAsync(AppDbContext dbContext, QuestRun run)
    {
        var runId = run.Id.Value;
        var existingEntities = await dbContext.QuestRunEnemies
            .Where(x => x.RunId == runId)
            .ToDictionaryAsync(x => x.EnemyInstanceId);

        SyncEntities(
            existingEntities,
            run.BattleState.Enemies,
            enemy => enemy.Id.Value,
            stale => dbContext.QuestRunEnemies.Remove(stale),
            enemy => dbContext.QuestRunEnemies.Add(ToEnemyEntity(run, enemy)),
            (existing, enemy) => ApplyEnemyEntity(existing, run, enemy));
    }

    private static async Task SyncTurnCommandsAsync(AppDbContext dbContext, QuestRun run)
    {
        var runId = run.Id.Value;
        var currentTurnNo = run.TurnState.CurrentTurnNo;

        var resolvedCommands = await dbContext.QuestTurnCommands
            .Where(x => x.RunId == runId && x.TurnNo < currentTurnNo)
            .ToListAsync();
        if (resolvedCommands.Count > 0)
        {
            dbContext.QuestTurnCommands.RemoveRange(resolvedCommands);
        }

        var existingCommands = await dbContext.QuestTurnCommands
            .Where(x => x.RunId == runId && x.TurnNo == currentTurnNo)
            .ToDictionaryAsync(x => x.ParticipantId);
        var pendingByParticipantId = run.TurnState.PendingCommands.ToDictionary(x => x.ParticipantId.Value);

        foreach (var stale in existingCommands.Values.Where(x => !pendingByParticipantId.ContainsKey(x.ParticipantId)))
        {
            dbContext.QuestTurnCommands.Remove(stale);
        }

        foreach (var command in run.TurnState.PendingCommands)
        {
            if (existingCommands.TryGetValue(command.ParticipantId.Value, out var existing))
            {
                existing.ActionKind = (int)command.ActionKind;
                existing.MoveId = command.MoveId?.Id;
                existing.TargetRow = command.SelectedTargetPosition is null ? null : (int)command.SelectedTargetPosition.Value.Row;
                existing.TargetColumn = command.SelectedTargetPosition is null ? null : (int)command.SelectedTargetPosition.Value.Column;
                existing.SubmittedAt = command.SubmittedAt;
                existing.IsAutoSubmitted = command.IsAutoSubmitted;
                continue;
            }

            dbContext.QuestTurnCommands.Add(ToTurnCommandEntity(runId, command));
        }
    }

    private static async Task SyncTrapsAsync(AppDbContext dbContext, QuestRun run)
    {
        var runId = run.Id.Value;
        var existingEntities = await dbContext.QuestFloorTraps
            .Where(x => x.RunId == runId)
            .ToDictionaryAsync(x => x.TrapId);

        SyncEntities(
            existingEntities,
            run.Traps.Traps,
            trap => trap.Id.Value,
            stale => dbContext.QuestFloorTraps.Remove(stale),
            trap => dbContext.QuestFloorTraps.Add(ToTrapEntity(runId, trap)),
            (existing, trap) =>
            {
                existing.SourceParticipantId = trap.SourceParticipantId.Value;
                existing.MoveId = trap.MoveId.Id;
                existing.ExpiresAfterFloorNo = trap.ExpiresAfterFloorNo;
                existing.IsTriggered = trap.IsTriggered;
            });
    }

    private static void SyncEntities<TEntity, TState, TKey>(
        IReadOnlyDictionary<TKey, TEntity> existingEntities,
        IReadOnlyList<TState> states,
        Func<TState, TKey> stateKeySelector,
        Action<TEntity> removeStale,
        Action<TState> addNew,
        Action<TEntity, TState> apply)
        where TKey : notnull
    {
        var statesByKey = states.ToDictionary(stateKeySelector);

        foreach (var (key, stale) in existingEntities)
        {
            if (!statesByKey.ContainsKey(key))
            {
                removeStale(stale);
            }
        }

        foreach (var state in states)
        {
            var key = stateKeySelector(state);
            if (existingEntities.TryGetValue(key, out var existing))
            {
                apply(existing, state);
                continue;
            }

            addNew(state);
        }
    }

    private static async Task SyncRewardSummaryAsync(AppDbContext dbContext, QuestRun run)
    {
        var runId = run.Id.Value;
        var existing = await dbContext.QuestRewardSummaries.SingleOrDefaultAsync(x => x.RunId == runId);
        if (existing is null)
        {
            dbContext.QuestRewardSummaries.Add(new QuestRewardSummaryEntity
            {
                RunId = runId,
                Exp = run.Rewards.Exp,
                Gold = run.Rewards.Gold,
                EquipmentRewardId = run.Rewards.EquipmentRewardId?.Value,
                ItemRewardId = run.Rewards.ItemRewardId?.Value,
                SkippedRewardPlayerIdsJson = SerializeRewardState(run.Rewards)
            });
            return;
        }

        existing.Exp = run.Rewards.Exp;
        existing.Gold = run.Rewards.Gold;
        existing.EquipmentRewardId = run.Rewards.EquipmentRewardId?.Value;
        existing.ItemRewardId = run.Rewards.ItemRewardId?.Value;
        existing.SkippedRewardPlayerIdsJson = SerializeRewardState(run.Rewards);
    }

    private static QuestRunEntity MapToEntity(QuestRun run) => new()
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
        EndedAt = run.EndedAt,
        Version = run.Version,
    };

    private static QuestRunPartyMemberEntity ToPartyMemberEntity(Guid runId, QuestRunPartyMemberState state)
    {
        var effectsJson = QuestJsonSerializer.SerializeEffects(state.Ailments, state.Buffs);
        return new QuestRunPartyMemberEntity
        {
            RunId = runId,
            ParticipantId = state.ParticipantId.Value,
            CurrentHp = state.CurrentHp,
            CurrentMp = state.CurrentMp,
            IsDead = state.IsDead,
            CanActFromTurn = state.CanActFromTurn,
            ActionMode = (int)state.ActionMode,
            HasLeftQuest = state.HasLeftQuest,
            IsManualControlRequested = state.IsManualControlRequested,
            PetSummonsUsed = state.PetSummonsUsed,
            ActiveEffectsJson = effectsJson,
            DerivedParametersJson = QuestJsonSerializer.SerializeDerivedParametersPlaceholder(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void ApplyPartyMemberEntity(QuestRunPartyMemberEntity entity, QuestRunPartyMemberState state)
    {
        var effectsJson = QuestJsonSerializer.SerializeEffects(state.Ailments, state.Buffs);
        entity.CurrentHp = state.CurrentHp;
        entity.CurrentMp = state.CurrentMp;
        entity.IsDead = state.IsDead;
        entity.CanActFromTurn = state.CanActFromTurn;
        entity.ActionMode = (int)state.ActionMode;
        entity.HasLeftQuest = state.HasLeftQuest;
        entity.IsManualControlRequested = state.IsManualControlRequested;
        entity.PetSummonsUsed = state.PetSummonsUsed;
        entity.ActiveEffectsJson = effectsJson;
        entity.DerivedParametersJson = QuestJsonSerializer.SerializeDerivedParametersPlaceholder();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static QuestRunEnemyEntity ToEnemyEntity(QuestRun run, QuestEnemyState enemy)
    {
        var effectsJson = QuestJsonSerializer.SerializeEffects(enemy.Ailments, enemy.Buffs);
        return new QuestRunEnemyEntity
        {
            RunId = run.Id.Value,
            EnemyInstanceId = enemy.Id.Value,
            FloorNo = run.FloorState.CurrentFloorNo,
            EnemyDefinitionId = enemy.EnemyDefinitionId.Value,
            BattleRow = (int)enemy.Position.Row,
            BattleColumn = (int)enemy.Position.Column,
            CurrentHp = enemy.CurrentHp,
            CurrentMp = enemy.CurrentMp,
            IsDead = enemy.IsDead,
            IsCaptured = enemy.IsCaptured,
            ActiveEffectsJson = effectsJson,
            DerivedParametersJson = QuestJsonSerializer.SerializeDerivedParametersPlaceholder()
        };
    }

    private static void ApplyEnemyEntity(QuestRunEnemyEntity entity, QuestRun run, QuestEnemyState enemy)
    {
        var effectsJson = QuestJsonSerializer.SerializeEffects(enemy.Ailments, enemy.Buffs);
        entity.FloorNo = run.FloorState.CurrentFloorNo;
        entity.EnemyDefinitionId = enemy.EnemyDefinitionId.Value;
        entity.BattleRow = (int)enemy.Position.Row;
        entity.BattleColumn = (int)enemy.Position.Column;
        entity.CurrentHp = enemy.CurrentHp;
        entity.CurrentMp = enemy.CurrentMp;
        entity.IsDead = enemy.IsDead;
        entity.IsCaptured = enemy.IsCaptured;
        entity.ActiveEffectsJson = effectsJson;
        entity.DerivedParametersJson = QuestJsonSerializer.SerializeDerivedParametersPlaceholder();
    }

    private static QuestTurnCommandEntity ToTurnCommandEntity(Guid runId, QuestSubmittedCommand command) => new()
    {
        RunId = runId,
        TurnNo = command.TurnNo,
        ParticipantId = command.ParticipantId.Value,
        ActionKind = (int)command.ActionKind,
        MoveId = command.MoveId?.Id,
        TargetRow = command.SelectedTargetPosition is null ? null : (int)command.SelectedTargetPosition.Value.Row,
        TargetColumn = command.SelectedTargetPosition is null ? null : (int)command.SelectedTargetPosition.Value.Column,
        SubmittedAt = command.SubmittedAt,
        IsAutoSubmitted = command.IsAutoSubmitted
    };

    private static QuestFloorTrapEntity ToTrapEntity(Guid runId, QuestTrapState trap) => new()
    {
        RunId = runId,
        TrapId = trap.Id.Value,
        SourceParticipantId = trap.SourceParticipantId.Value,
        MoveId = trap.MoveId.Id,
        ExpiresAfterFloorNo = trap.ExpiresAfterFloorNo,
        IsTriggered = trap.IsTriggered,
    };

    private static async Task<QuestRun> LoadAsync(AppDbContext dbContext, QuestRunEntity runEntity)
    {
        var runId = runEntity.Id;
        var partyMemberEntities = await dbContext.QuestRunPartyMembers
            .AsNoTracking()
            .Where(x => x.RunId == runId)
            .ToListAsync();
        var snapshotEntities = await dbContext.QuestRunPartySnapshots
            .AsNoTracking()
            .Where(x => x.RunId == runId)
            .ToListAsync();
        var enemyEntities = await dbContext.QuestRunEnemies
            .AsNoTracking()
            .Where(x => x.RunId == runId)
            .ToListAsync();
        var turnCommandEntities = await dbContext.QuestTurnCommands
            .AsNoTracking()
            .Where(x => x.RunId == runId && x.TurnNo == runEntity.CurrentTurnNo)
            .ToListAsync();
        var trapEntities = await dbContext.QuestFloorTraps
            .AsNoTracking()
            .Where(x => x.RunId == runId)
            .ToListAsync();
        var rewardEntity = await dbContext.QuestRewardSummaries
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.RunId == runId);

        return MapToDomain(
            runEntity,
            snapshotEntities,
            partyMemberEntities,
            enemyEntities,
            turnCommandEntities,
            trapEntities,
            rewardEntity);
    }

    private static QuestRun MapToDomain(
        QuestRunEntity runEntity,
        IReadOnlyCollection<QuestRunPartySnapshotEntity> snapshotEntities,
        IReadOnlyCollection<QuestRunPartyMemberEntity> partyMemberEntities,
        IReadOnlyCollection<QuestRunEnemyEntity> enemyEntities,
        IReadOnlyCollection<QuestTurnCommandEntity> turnCommandEntities,
        IReadOnlyCollection<QuestFloorTrapEntity> trapEntities,
        QuestRewardSummaryEntity? rewardEntity)
    {
        var snapshots = snapshotEntities.Select(MapSnapshot).ToArray();
        var partyMembers = partyMemberEntities.Select(MapPartyMember).ToArray();
        var enemies = enemyEntities.Select(MapEnemy).ToArray();
        var turnCommands = turnCommandEntities.Select(MapTurnCommand).ToArray();
        var traps = trapEntities.Select(MapTrap).ToArray();
        var lastTurnResults = QuestJsonSerializer.DeserializeLastTurnResults(runEntity.LastTurnResultsJson);
        var chatMessages = QuestJsonSerializer.DeserializeChatMessages(runEntity.ChatMessagesJson);

        var rewardState = DeserializeRewardState(rewardEntity?.SkippedRewardPlayerIdsJson);

        return new QuestRun(
            new QuestRunId(runEntity.Id),
            new QuestRoomId(runEntity.RoomId),
            new QuestStageId(runEntity.StageId),
            snapshots,
            new QuestFloorState(runEntity.CurrentFloorNo, false, []),
            new QuestBattleState(partyMembers, enemies),
            new QuestTurnState(runEntity.CurrentTurnNo, runEntity.ActionDeadlineAt, turnCommands, runEntity.LastResolvedTurnNo),
            new QuestTrapCollection(traps),
            new QuestRewardAccumulator(
                rewardEntity?.Exp ?? 0,
                rewardEntity?.EquipmentRewardId is null ? null : new EquipmentId(rewardEntity.EquipmentRewardId.Value),
                rewardEntity?.ItemRewardId is null ? null : new ItemId(rewardEntity.ItemRewardId.Value),
                rewardEntity?.Gold ?? 0,
                rewardState.SkippedRewardPlayerIds,
                rewardState.CapturedPets),
            lastTurnResults,
            chatMessages,
            runEntity.StartedAt,
            (QuestRunStatus)runEntity.Status,
            runEntity.EndedAt,
            runEntity.Version);
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
            buffs,
            entity.PetSummonsUsed);
    }

    private static QuestRunPartyMemberSnapshot MapSnapshot(QuestRunPartySnapshotEntity entity)
    {
        QuestPetSnapshot? pet = null;
        if (entity.PetEnemyDefinitionId is not null && entity.PetMaxHp is not null)
        {
            pet = new QuestPetSnapshot(
                new QuestEnemyDefinitionId(entity.PetEnemyDefinitionId.Value),
                new Status(
                    entity.PetMaxHp.Value,
                    entity.PetMaxMp ?? 0,
                    entity.PetStrength ?? 0,
                    entity.PetDefense ?? 0,
                    entity.PetIntelligence ?? 0,
                    entity.PetLuck ?? 0,
                    entity.PetSpeed ?? 0));
        }

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
            (ActionMode)entity.InitialActionMode,
            pet);
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
            buffs,
            entity.IsCaptured);
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

    private static string SerializeRewardState(QuestRewardAccumulator rewards)
    {
        return JsonSerializer.Serialize(new QuestRewardStateDto(
            rewards.SkippedRewardPlayerIds.Select(x => x.Value).ToArray(),
            rewards.CapturedPetRewards.Select(x => new CapturedPetDto(
                x.PlayerId.Value,
                x.EnemyDefinitionId.Value,
                x.CapturedAt)).ToArray()));
    }

    private static QuestRewardState DeserializeRewardState(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return QuestRewardState.Empty;
        }

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            var playerIds = JsonSerializer.Deserialize<Guid[]>(json) ?? [];
            return new QuestRewardState(
                playerIds.Select(x => new PlayerId(x)).ToArray(),
                []);
        }

        var state = JsonSerializer.Deserialize<QuestRewardStateDto>(json);
        if (state is null)
        {
            return QuestRewardState.Empty;
        }

        return new QuestRewardState(
            (state.SkippedRewardPlayerIds ?? [])
                .Select(x => new PlayerId(x))
                .ToArray(),
            (state.CapturedPets ?? [])
                .Select(x => new QuestCapturedPetReward(
                    new PlayerId(x.PlayerId),
                    new QuestEnemyDefinitionId(x.EnemyDefinitionId),
                    x.CapturedAt))
                .ToArray());
    }

    // 解決/自動処理のホットパス向け軽量射影。表示専用で重い last_turn_results_json を読み込まない。
    private static readonly Expression<Func<QuestRunEntity, LightRunColumns>> LightRunProjection =
        x => new LightRunColumns(
            x.Id,
            x.RoomId,
            x.StageId,
            x.Status,
            x.CurrentFloorNo,
            x.CurrentTurnNo,
            x.ActionDeadlineAt,
            x.LastResolvedTurnNo,
            x.ChatMessagesJson,
            x.StartedAt,
            x.EndedAt,
            x.Version);

    private static QuestRunEntity ToLightRunEntity(LightRunColumns columns) => new()
    {
        Id = columns.Id,
        RoomId = columns.RoomId,
        StageId = columns.StageId,
        Status = columns.Status,
        CurrentFloorNo = columns.CurrentFloorNo,
        CurrentTurnNo = columns.CurrentTurnNo,
        ActionDeadlineAt = columns.ActionDeadlineAt,
        LastResolvedTurnNo = columns.LastResolvedTurnNo,
        // 未取得を表す。MapToDomain で LastTurnResults=null となり、保存時もダーティでない限り上書きしない。
        LastTurnResultsJson = null,
        ChatMessagesJson = columns.ChatMessagesJson,
        StartedAt = columns.StartedAt,
        EndedAt = columns.EndedAt,
        Version = columns.Version
    };

    private sealed record LightRunColumns(
        Guid Id,
        Guid RoomId,
        int StageId,
        int Status,
        int CurrentFloorNo,
        int CurrentTurnNo,
        DateTimeOffset ActionDeadlineAt,
        int? LastResolvedTurnNo,
        string ChatMessagesJson,
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt,
        int Version);

    private sealed record QuestRewardStateDto(Guid[]? SkippedRewardPlayerIds, CapturedPetDto[]? CapturedPets);

    private sealed record CapturedPetDto(Guid PlayerId, int EnemyDefinitionId, DateTimeOffset CapturedAt);

    private sealed record QuestRewardState(
        IReadOnlyList<PlayerId> SkippedRewardPlayerIds,
        IReadOnlyList<QuestCapturedPetReward> CapturedPets)
    {
        public static QuestRewardState Empty { get; } = new([], []);
    }
}
