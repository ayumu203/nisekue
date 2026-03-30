using server.application.battle;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestBattleFactory
{
    private readonly QuestAllyNpcActionPolicy allyNpcActionPolicy = new();
    private readonly QuestEnemyActionPolicy enemyActionPolicy = new();

    public BattleActorInput[] CreateActorInputs(
        QuestRun run,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition>? enemyDefinitions = null)
    {
        ArgumentNullException.ThrowIfNull(run);

        var party = run.PartySnapshots
            .Join(
                run.BattleState.PartyMembers,
                snapshot => snapshot.ParticipantId,
                state => state.ParticipantId,
                (snapshot, state) => new BattleActorInput(
                    ActorId: snapshot.ParticipantId.Value,
                    DisplayName: snapshot.DisplayName,
                    Side: BattleSide.Ally,
                    BaseStatus: snapshot.BaseStatus,
                    MoveSet: snapshot.MoveSet,
                    CurrentHp: state.CurrentHp,
                    CurrentMp: state.CurrentMp,
                    Ailments: state.Ailments.ToArray(),
                    Buffs: state.Buffs.ToArray()))
            .ToArray();

        var enemy = run.BattleState.Enemies
            .Select(x =>
            {
                var definition = ResolveEnemyDefinition(x, enemyDefinitions);
                return new BattleActorInput(
                    ActorId: x.Id.Value,
                    DisplayName: definition.Name,
                    Side: BattleSide.Enemy,
                    BaseStatus: definition.Status,
                    MoveSet: CreateMoveSet(definition.MoveIds),
                    CurrentHp: x.CurrentHp,
                    CurrentMp: x.CurrentMp,
                    Ailments: x.Ailments.ToArray(),
                    Buffs: x.Buffs.ToArray());
            })
            .ToArray();

        return party.Concat(enemy).ToArray();
    }

    public BattleFieldContext CreateBattleFieldContext(QuestRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var partyPositions = run.PartySnapshots
            .Select(x => new BattleActorPosition(new BattleActorId(x.ParticipantId.Value), x.StartPosition));
        var enemyPositions = run.BattleState.Enemies
            .Select(x => new BattleActorPosition(new BattleActorId(x.Id.Value), x.Position));
        var bossActorIds = run.FloorState.IsBossFloor
            ? run.BattleState.Enemies.Select(x => new BattleActorId(x.Id.Value)).ToArray()
            : [];

        return new BattleFieldContext(partyPositions.Concat(enemyPositions).ToArray(), bossActorIds);
    }

    public IReadOnlyDictionary<BattleActorId, QuestParticipantId> CreatePartyActorMap(QuestRun run)
    {
        return run.PartySnapshots.ToDictionary(
            x => new BattleActorId(x.ParticipantId.Value),
            x => x.ParticipantId);
    }

    public IReadOnlyDictionary<BattleActorId, QuestEnemyInstanceId> CreateEnemyActorMap(QuestRun run)
    {
        return run.BattleState.Enemies.ToDictionary(
            x => new BattleActorId(x.Id.Value),
            x => x.Id);
    }

    public async Task<(BattleActionInput[] Actions, Move[] Moves)> CreateTurnInputsAsync(
        QuestRun run,
        IMoveRepository moveRepository,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition>? enemyDefinitions = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(moveRepository);

        var pending = run.TurnState.PendingCommands.ToDictionary(x => x.ParticipantId);
        var partyActions = new List<BattleActionInput>();
        var movesById = new Dictionary<int, Move>();

        foreach (var member in run.BattleState.PartyMembers)
        {
            if (member.IsDead || member.HasLeftQuest || member.CanActFromTurn > run.TurnState.CurrentTurnNo)
            {
                continue;
            }

            var command = pending.TryGetValue(member.ParticipantId, out var submitted)
                ? submitted
                : await CreateFallbackPartyCommandAsync(run, member, moveRepository, movesById);

            Move? move = null;
            if (command.ActionKind == ActionKind.UseMove && command.MoveId is not null)
            {
                move = await GetMoveAsync(command.MoveId, moveRepository, movesById);
            }

            var action = CreateBattleAction(command, move);
            if (action is not null)
            {
                partyActions.Add(action);
            }
        }

        var enemyActions = new List<BattleActionInput>();
        foreach (var enemy in run.BattleState.Enemies.Where(x => !x.IsDead))
        {
            var definition = ResolveEnemyDefinition(enemy, enemyDefinitions);
            var availableMoves = new List<Move>();
            foreach (var moveId in definition.MoveIds)
            {
                availableMoves.Add(await GetMoveAsync(moveId, moveRepository, movesById));
            }

            enemyActions.Add(enemyActionPolicy.SelectAction(run, enemy, definition, availableMoves, enemyDefinitions));
        }

        return (partyActions.Concat(enemyActions).ToArray(), movesById.Values.ToArray());
    }

    private static QuestSubmittedCommand CreateFallbackPartyCommand(QuestRun run, QuestRunPartyMemberState member)
    {
        var snapshot = run.PartySnapshots.FirstOrDefault(x => x.ParticipantId == member.ParticipantId)
            ?? throw new KeyNotFoundException($"参加者スナップショットが見つかりません。 participantId={member.ParticipantId.Value}");

        if (snapshot.Type == ParticipantType.Npc)
        {
            throw new InvalidOperationException("NPC のフォールバックコマンドには行動ポリシーを使用してください。");
        }

        var reachableRows = QuestBattleReachability.GetReachableRows(snapshot.StartPosition.Row);
        var selectedTargetPosition = run.BattleState.Enemies
            .Where(x => !x.IsDead)
            .Where(x => reachableRows.Contains(x.Position.Row))
            .OrderBy(x => (int)x.Position.Row)
            .ThenBy(x => (int)x.Position.Column)
            .Select(x => (BattlePosition?)x.Position)
            .FirstOrDefault();

        if (selectedTargetPosition is null)
        {
            return new QuestSubmittedCommand(
                member.ParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.Wait,
                DateTimeOffset.UtcNow,
                isAutoSubmitted: true);
        }

        return new QuestSubmittedCommand(
            member.ParticipantId,
            run.TurnState.CurrentTurnNo,
            ActionKind.NormalAttack,
            DateTimeOffset.UtcNow,
            selectedTargetPosition: selectedTargetPosition,
            isAutoSubmitted: true);
    }

    private async Task<QuestSubmittedCommand> CreateFallbackPartyCommandAsync(
        QuestRun run,
        QuestRunPartyMemberState member,
        IMoveRepository moveRepository,
        IDictionary<int, Move> movesById)
    {
        var snapshot = run.PartySnapshots.FirstOrDefault(x => x.ParticipantId == member.ParticipantId)
            ?? throw new KeyNotFoundException($"参加者スナップショットが見つかりません。 participantId={member.ParticipantId.Value}");
        if (snapshot.Type != ParticipantType.Npc)
        {
            return CreateFallbackPartyCommand(run, member);
        }

        var availableMoves = new List<Move>();
        foreach (var moveId in snapshot.MoveSet.GetLearnedMoveIds())
        {
            availableMoves.Add(await GetMoveAsync(moveId, moveRepository, movesById));
        }

        return allyNpcActionPolicy.SelectAction(run, member.ParticipantId, availableMoves, DateTimeOffset.UtcNow);
    }

    private static BattleActionInput? CreateBattleAction(QuestSubmittedCommand command, Move? move = null)
    {
        return command.ActionKind switch
        {
            ActionKind.NormalAttack => new BattleActionInput(
                ActorId: command.ParticipantId.Value,
                Kind: BattleActionKind.NormalAttack,
                MoveId: null,
                TargetType: TargetType.Enemy,
                AttackRange: AttackRange.Single,
                SelectedPosition: command.SelectedTargetPosition),
            ActionKind.UseMove when command.MoveId is not null && move is not null => new BattleActionInput(
                ActorId: command.ParticipantId.Value,
                Kind: BattleActionKind.UseMove,
                MoveId: command.MoveId.Id,
                TargetType: move.TargetType,
                AttackRange: move.AttackRange,
                SelectedPosition: command.SelectedTargetPosition),
            ActionKind.Prayer => new BattleActionInput(
                ActorId: command.ParticipantId.Value,
                Kind: BattleActionKind.Prayer,
                MoveId: null,
                TargetType: TargetType.Ally,
                AttackRange: AttackRange.Single,
                SelectedPosition: command.SelectedTargetPosition),
            ActionKind.Guard => new BattleActionInput(
                ActorId: command.ParticipantId.Value,
                Kind: BattleActionKind.Guard,
                MoveId: null,
                TargetType: TargetType.Self,
                AttackRange: AttackRange.Single),
            ActionKind.Wait => new BattleActionInput(
                ActorId: command.ParticipantId.Value,
                Kind: BattleActionKind.Wait,
                MoveId: null,
                TargetType: TargetType.Self,
                AttackRange: AttackRange.Single),
            _ => null
        };
    }

    private static QuestEnemyDefinition ResolveEnemyDefinition(
        QuestEnemyState enemy,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition>? enemyDefinitions)
    {
        if (enemyDefinitions is not null && enemyDefinitions.TryGetValue(enemy.EnemyDefinitionId, out var definition))
        {
            return definition;
        }

        return new QuestEnemyDefinition(
            enemy.EnemyDefinitionId,
            $"Enemy-{enemy.EnemyDefinitionId.Value}",
            level: 1,
            new server.domain.player.Status(
                maxHp: Math.Max(1, enemy.CurrentHp),
                maxMp: Math.Max(0, enemy.CurrentMp),
                strength: 1,
                defense: 1,
                intelligence: 1,
                luck: 1,
                speed: 1),
            imagePath: "/image/battle/placeholder.png",
            aiType: EnemyAiType.Aggressive,
            moveIds: []);
    }

    private static MoveSet CreateMoveSet(IEnumerable<MoveId> moveIds)
    {
        var moveSet = new MoveSet();
        var orderedMoveIds = moveIds.ToArray();
        for (var i = 0; i < orderedMoveIds.Length && i < MoveSet.MaxSlots; i++)
        {
            moveSet.SetSlot(i, orderedMoveIds[i]);
        }

        return moveSet;
    }

    private static async Task<Move> GetMoveAsync(
        MoveId moveId,
        IMoveRepository moveRepository,
        IDictionary<int, Move> movesById)
    {
        if (movesById.TryGetValue(moveId.Id, out var cached))
        {
            return cached;
        }

        var move = await moveRepository.GetMoveAsync(moveId)
            ?? throw new KeyNotFoundException($"スキル定義が見つかりません。 moveId={moveId.Id}");
        movesById[moveId.Id] = move;
        return move;
    }
}
