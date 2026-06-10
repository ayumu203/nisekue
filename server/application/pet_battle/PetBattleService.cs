using server.application.battle;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.pet;
using server.domain.pet_battle;
using server.domain.pet_battle.enums;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.pet_battle;

public class PetBattleService(
    IPetBattleRoomRepository roomRepository,
    IPetBattleRunRepository runRepository,
    IPlayerPetBattleStatsRepository statsRepository,
    IMatchingCandidateRepository matchingCandidateRepository,
    IPlayerRepository playerRepository,
    IPlayerPetRepository playerPetRepository,
    IQuestEnemyDefinitionRepository questEnemyDefinitionRepository,
    IMoveRepository moveRepository,
    PetBattleSnapshotFactory snapshotFactory,
    PetBattleRunFactory runFactory,
    BattleService battleService)
{
    private static readonly TimeSpan TurnDeadline = TimeSpan.FromSeconds(PetBattleConstants.TurnDeadlineSeconds);
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(PetBattleConstants.CooldownMinutes);
    private readonly PetBattleEnemyActionPolicy _enemyPolicy = new();

    public async Task<PetBattleRoom> MatchAsync(PlayerId ownerId)
    {
        var now = DateTimeOffset.UtcNow;

        var owner = await playerRepository.GetPlayerAsync(ownerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

        if (owner.PetBattleCooldownUntil is not null && owner.PetBattleCooldownUntil.Value > now)
        {
            var remaining = (int)Math.Ceiling((owner.PetBattleCooldownUntil.Value - now).TotalSeconds);
            throw new InvalidOperationException($"クールダウン中です。あと{remaining}秒お待ちください。");
        }

        var existingRun = await runRepository.GetActiveByOwnerAsync(ownerId);
        if (existingRun is not null)
        {
            throw new InvalidOperationException("すでに対戦中です。");
        }

        var existingRoom = await roomRepository.GetActiveByOwnerAsync(ownerId);
        if (existingRoom is not null)
        {
            return existingRoom;
        }

        var ownerPets = await playerPetRepository.GetByPlayerAsync(ownerId);
        if (ownerPets.Count == 0)
        {
            throw new InvalidOperationException("ペットを所持していないため対戦できません。");
        }

        var ownerStats = await statsRepository.GetByPlayerIdAsync(ownerId)
            ?? PlayerPetBattleStats.CreateInitial(ownerId, now);

        var candidates = await matchingCandidateRepository.GetCandidatesAsync(ownerId);
        var opponentId = PetBattleMatchingService.SelectOpponent(ownerId, ownerStats.Rating, candidates)
            ?? throw new InvalidOperationException("対戦相手が見つかりません。ペットを所持している他のプレイヤーが必要です。");

        var room = PetBattleRoom.Create(ownerId, opponentId, now);
        await roomRepository.SaveAsync(room);
        return room;
    }

    public async Task<PetBattleRoom> GetRoomAsync(PetBattleRoomId roomId)
    {
        return await roomRepository.GetAsync(roomId)
            ?? throw new KeyNotFoundException("対戦ルームが見つかりません。");
    }

    public async Task<PetBattleRoom> AssignSlotAsync(
        PetBattleRoomId roomId,
        PlayerPetId petId,
        BattleRow row,
        BattleColumn column,
        PlayerId ownerId)
    {
        var room = await GetRoomAsync(roomId);
        EnsureOwner(room, ownerId);
        room.AssignSlot(petId, row, column);
        await roomRepository.SaveAsync(room);
        return room;
    }

    public async Task<PetBattleRoom> RemoveSlotAsync(PetBattleRoomId roomId, PlayerPetId petId, PlayerId ownerId)
    {
        var room = await GetRoomAsync(roomId);
        EnsureOwner(room, ownerId);
        room.RemoveSlot(petId);
        await roomRepository.SaveAsync(room);
        return room;
    }

    public async Task<PetBattleRun> StartBattleAsync(PetBattleRoomId roomId, PlayerId ownerId)
    {
        var now = DateTimeOffset.UtcNow;
        var room = await GetRoomAsync(roomId);
        EnsureOwner(room, ownerId);

        if (!room.CanStart())
        {
            throw new InvalidOperationException("対戦を開始するにはペットを最低1体配置してください。");
        }

        var ownerPets = await playerPetRepository.GetByPlayerAsync(ownerId);
        var opponentPets = await playerPetRepository.GetByPlayerAsync(room.OpponentPlayerId);

        if (opponentPets.Count == 0)
        {
            room.Cancel(now);
            await roomRepository.SaveAsync(room);
            throw new InvalidOperationException("相手のペットがいなくなったため対戦を開始できません。");
        }

        var allEnemyDefs = (await questEnemyDefinitionRepository.GetAllAsync())
            .ToDictionary(x => x.Id.Value);

        var ownerSnapshots = snapshotFactory.CreateOwnerSnapshots(room.Slots, ownerPets, allEnemyDefs);
        var opponentSnapshots = snapshotFactory.CreateOpponentSnapshots(opponentPets, allEnemyDefs);

        if (ownerSnapshots.Length == 0)
        {
            throw new InvalidOperationException("有効なペットが配置されていません。");
        }

        room.CloseForStart(now);
        var run = runFactory.Create(room, ownerSnapshots, opponentSnapshots, now);

        await roomRepository.SaveAsync(room);
        await runRepository.SaveAsync(run);

        return run;
    }

    public async Task<PetBattleRun> GetRunAsync(PetBattleRunId runId)
    {
        return await runRepository.GetAsync(runId)
            ?? throw new KeyNotFoundException("対戦進行情報が見つかりません。");
    }

    public async Task<(PetBattleRoom? Room, PetBattleRun? Run)> GetStatusAsync(PlayerId playerId)
    {
        var run = await runRepository.GetActiveByOwnerAsync(playerId);
        if (run is not null)
        {
            return (null, run);
        }

        var room = await roomRepository.GetActiveByOwnerAsync(playerId);
        return (room, null);
    }

    public async Task<PlayerPetBattleStats?> GetStatsAsync(PlayerId playerId)
    {
        return await statsRepository.GetByPlayerIdAsync(playerId);
    }

    public async Task<PetBattleCommandSubmissionResult> SubmitCommandAsync(
        PetBattleRunId runId,
        PetBattleParticipantId participantId,
        PetBattleSubmittedCommand command,
        PlayerId ownerId)
    {
        var run = await GetRunAsync(runId);

        if (run.OwnerPlayerId != ownerId)
        {
            throw new UnauthorizedAccessException("このバトルのコマンドを操作する権限がありません。");
        }

        run.SubmitCommand(participantId, command, DateTimeOffset.UtcNow);

        var resolved = false;
        if (run.AllOwnerCommandsSubmitted())
        {
            resolved = await TryResolveTurnAsync(run);
        }

        await runRepository.SaveAsync(run);

        if (run.IsFinished)
        {
            await UpdateStatsAsync(run);
        }

        return new PetBattleCommandSubmissionResult(run, resolved);
    }

    public async Task<PetBattleRun> AbortAsync(PetBattleRunId runId, PlayerId ownerId)
    {
        var now = DateTimeOffset.UtcNow;
        var run = await GetRunAsync(runId);

        if (run.OwnerPlayerId != ownerId)
        {
            throw new UnauthorizedAccessException("このバトルを中断する権限がありません。");
        }

        run.Abort(now);
        await runRepository.SaveAsync(run);

        var ownerStats = await GetOrCreateStatsAsync(run.OwnerPlayerId, now);
        ownerStats.ApplyLoss(now);
        await statsRepository.UpsertAsync(ownerStats);

        var owner = await playerRepository.GetPlayerAsync(run.OwnerPlayerId);
        if (owner is not null)
        {
            owner.SetPetBattleCooldownUntil(now.Add(Cooldown));
            await playerRepository.SaveAsync(owner);
        }

        return run;
    }

    public async Task<IReadOnlyList<PetBattleRun>> ProcessExpiredRunsAsync(DateTimeOffset now)
    {
        var expiredRuns = await runRepository.ListExpiredAsync(now);
        if (expiredRuns.Count == 0)
        {
            return [];
        }

        var updatedRuns = new List<PetBattleRun>();
        foreach (var run in expiredRuns)
        {
            run.SwitchToAutoActionForTimeout(now);
            var resolved = await TryResolveTurnAsync(run);
            if (resolved)
            {
                await runRepository.SaveAsync(run);
                if (run.IsFinished)
                {
                    await UpdateStatsAsync(run);
                }

                updatedRuns.Add(run);
            }
        }

        return updatedRuns;
    }

    private async Task<bool> TryResolveTurnAsync(PetBattleRun run)
    {
        var now = DateTimeOffset.UtcNow;
        var actors = BuildActorInputs(run);
        var fieldContext = BuildFieldContext(run);
        var (actions, moves) = await BuildTurnInputsAsync(run);

        var resolution = battleService.ResolveTurn(new BattleTurnRequest(actors, actions, moves, fieldContext));
        var lastTurnResults = BuildLastTurnResults(run, resolution, moves, now);

        var ownerStates = resolution.UpdatedStates
            .Where(s => run.OwnerSnapshots.Any(snap => snap.ParticipantId.Value == s.Id.Value))
            .ToDictionary(s => new PetBattleParticipantId(s.Id.Value));

        var opponentStates = resolution.UpdatedStates
            .Where(s => run.OpponentSnapshots.Any(snap => snap.ParticipantId.Value == s.Id.Value))
            .ToDictionary(s => new PetBattleParticipantId(s.Id.Value));

        run.ApplyBattleResolution(ownerStates, opponentStates, lastTurnResults, now.Add(TurnDeadline));
        return true;
    }

    private static BattleActorInput[] BuildActorInputs(PetBattleRun run)
    {
        var ownerActors = run.OwnerSnapshots
            .Join(run.OwnerMemberStates, s => s.ParticipantId, m => m.ParticipantId,
                (s, m) => new BattleActorInput(
                    s.ParticipantId.Value, s.DisplayName, BattleSide.Ally, s.BaseStatus, s.BuildMoveSet(),
                    m.CurrentHp, m.CurrentMp, m.Ailments.ToArray(), m.Buffs.ToArray()))
            .ToArray();

        var opponentActors = run.OpponentSnapshots
            .Join(run.OpponentMemberStates, s => s.ParticipantId, m => m.ParticipantId,
                (s, m) => new BattleActorInput(
                    s.ParticipantId.Value, s.DisplayName, BattleSide.Enemy, s.BaseStatus, s.BuildMoveSet(),
                    m.CurrentHp, m.CurrentMp, m.Ailments.ToArray(), m.Buffs.ToArray()))
            .ToArray();

        return ownerActors.Concat(opponentActors).ToArray();
    }

    private static BattleFieldContext BuildFieldContext(PetBattleRun run)
    {
        var positions = run.OwnerSnapshots
            .Select(s => new BattleActorPosition(new BattleActorId(s.ParticipantId.Value), s.StartPosition))
            .Concat(run.OpponentSnapshots
                .Select(s => new BattleActorPosition(new BattleActorId(s.ParticipantId.Value), s.StartPosition)))
            .ToArray();

        return new BattleFieldContext(positions, []);
    }

    private async Task<(BattleActionInput[] Actions, Move[] Moves)> BuildTurnInputsAsync(PetBattleRun run)
    {
        var movesById = new Dictionary<int, Move>();
        var actions = new List<BattleActionInput>();
        var pendingById = run.TurnState.PendingCommands.ToDictionary(c => c.ParticipantId);

        foreach (var member in run.OwnerMemberStates)
        {
            if (member.IsDead || member.CanActFromTurn > run.TurnState.CurrentTurnNo)
            {
                continue;
            }

            BattleActionInput? action = null;
            if (pendingById.TryGetValue(member.ParticipantId, out var command))
            {
                Move? move = null;
                if (command.ActionKind == ActionKind.UseMove && command.MoveId is not null)
                {
                    move = await GetOrLoadMoveAsync(command.MoveId.Id, movesById);
                }

                action = CreateActionFromCommand(command, move);
            }

            action ??= new BattleActionInput(
                member.ParticipantId.Value, BattleActionKind.NormalAttack, null, TargetType.Enemy, AttackRange.Single);
            actions.Add(action);
        }

        foreach (var member in run.OpponentMemberStates)
        {
            if (member.IsDead || member.CanActFromTurn > run.TurnState.CurrentTurnNo)
            {
                continue;
            }

            var snapshot = run.OpponentSnapshots.FirstOrDefault(s => s.ParticipantId == member.ParticipantId);
            if (snapshot is null)
            {
                continue;
            }

            var availableMoves = new List<Move>();
            foreach (var moveId in snapshot.MoveIds)
            {
                availableMoves.Add(await GetOrLoadMoveAsync(moveId, movesById));
            }

            actions.Add(_enemyPolicy.SelectAction(snapshot, run.OwnerMemberStates, run.OwnerSnapshots, availableMoves));
        }

        return (actions.ToArray(), movesById.Values.ToArray());
    }

    private async Task<Move> GetOrLoadMoveAsync(int moveId, IDictionary<int, Move> movesById)
    {
        if (movesById.TryGetValue(moveId, out var cached))
        {
            return cached;
        }

        var move = await moveRepository.GetMoveAsync(new MoveId(moveId))
            ?? throw new KeyNotFoundException($"スキルが見つかりません。 moveId={moveId}");
        movesById[moveId] = move;
        return move;
    }

    private static BattleActionInput? CreateActionFromCommand(PetBattleSubmittedCommand command, Move? move)
    {
        return command.ActionKind switch
        {
            ActionKind.NormalAttack => new BattleActionInput(
                command.ParticipantId.Value, BattleActionKind.NormalAttack, null,
                TargetType.Enemy, AttackRange.Single, SelectedPosition: command.SelectedTargetPosition),
            ActionKind.UseMove when command.MoveId is not null && move is not null => new BattleActionInput(
                command.ParticipantId.Value, BattleActionKind.UseMove, command.MoveId.Id,
                move.TargetType, move.AttackRange, SelectedPosition: command.SelectedTargetPosition),
            ActionKind.Guard => new BattleActionInput(
                command.ParticipantId.Value, BattleActionKind.Guard, null, TargetType.Self, AttackRange.Single),
            ActionKind.Wait => new BattleActionInput(
                command.ParticipantId.Value, BattleActionKind.Wait, null, TargetType.Self, AttackRange.Single),
            _ => null
        };
    }

    private static PetBattleLastTurnResults BuildLastTurnResults(
        PetBattleRun run,
        BattleTurnResolution resolution,
        IReadOnlyList<Move> moves,
        DateTimeOffset now)
    {
        var allSnapshots = run.OwnerSnapshots
            .Concat(run.OpponentSnapshots)
            .ToDictionary(s => s.ParticipantId.Value);
        var moveById = moves
            .GroupBy(m => m.Id.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var actions = resolution.ActionResults
            .Where(r => !r.IsTurnEndEffect)
            .Select(actionResult =>
            {
                var actorName = allSnapshots.TryGetValue(actionResult.ActorId.Value, out var actorSnap)
                    ? actorSnap.DisplayName
                    : actionResult.ActorId.Value.ToString();

                var targetSummaries = actionResult.TargetResults.Select(tr =>
                {
                    var targetName = allSnapshots.TryGetValue(tr.TargetActorId.Value, out var targetSnap)
                        ? targetSnap.DisplayName
                        : tr.TargetActorId.Value.ToString();

                    var resultType = tr.IsDefeated ? "Defeated"
                        : tr.AppliedAilment is not null ? "AilmentApplied"
                        : tr.Damage > 0 || tr.HpChange != 0 || tr.MpChange != 0 ? "Hit"
                        : "Miss";

                    string[] appliedEffects = tr.AppliedAilment is null ? [] : [tr.AppliedAilment.Value.ToString()];

                    return new QuestResolvedTargetSummary(
                        targetParticipantId: tr.TargetActorId.Value,
                        targetEnemyInstanceId: null,
                        targetName,
                        resultType,
                        tr.HpChange,
                        tr.MpChange,
                        appliedEffects,
                        [],
                        tr.IsDefeated);
                }).ToArray();

                var move = actionResult.MoveId is null ? null : moveById.GetValueOrDefault(actionResult.MoveId.Id);

                return new QuestResolvedAction(
                    actorParticipantId: actionResult.ActorId.Value,
                    actorEnemyInstanceId: null,
                    actorName,
                    MapActionKind(actionResult.ActionKind),
                    actionResult.MoveId?.Id,
                    move?.Name,
                    actionResult.Succeeded,
                    targetSummaries);
            })
            .ToArray();

        return new PetBattleLastTurnResults(run.TurnState.CurrentTurnNo, now, actions);
    }

    private static string MapActionKind(BattleActionKind kind) => kind switch
    {
        BattleActionKind.NormalAttack => "NormalAttack",
        BattleActionKind.UseMove => "UseMove",
        BattleActionKind.Prayer => "Prayer",
        BattleActionKind.Guard => "Guard",
        BattleActionKind.Wait => "Wait",
        _ => kind.ToString()
    };

    private async Task UpdateStatsAsync(PetBattleRun run)
    {
        var now = DateTimeOffset.UtcNow;

        var ownerStats = await GetOrCreateStatsAsync(run.OwnerPlayerId, now);
        var opponentStats = await GetOrCreateStatsAsync(run.OpponentPlayerId, now);

        if (run.WinnerPlayerId == run.OwnerPlayerId)
        {
            ownerStats.ApplyWin(now);
            opponentStats.ApplyLoss(now);
        }
        else if (run.WinnerPlayerId == run.OpponentPlayerId)
        {
            ownerStats.ApplyLoss(now);
            opponentStats.ApplyWin(now);
        }

        await statsRepository.UpsertAsync(ownerStats);
        await statsRepository.UpsertAsync(opponentStats);

        var owner = await playerRepository.GetPlayerAsync(run.OwnerPlayerId);
        if (owner is not null)
        {
            owner.SetPetBattleCooldownUntil(now.Add(Cooldown));
            await playerRepository.SaveAsync(owner);
        }
    }

    private async Task<PlayerPetBattleStats> GetOrCreateStatsAsync(PlayerId playerId, DateTimeOffset now)
    {
        return await statsRepository.GetByPlayerIdAsync(playerId)
            ?? PlayerPetBattleStats.CreateInitial(playerId, now);
    }

    private static void EnsureOwner(PetBattleRoom room, PlayerId playerId)
    {
        if (room.OwnerPlayerId != playerId)
        {
            throw new UnauthorizedAccessException("このルームのオーナーではありません。");
        }
    }
}
