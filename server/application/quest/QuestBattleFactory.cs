using server.application.battle;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestBattleFactory
{
    private readonly QuestAllyNpcActionPolicy allyNpcActionPolicy = new();

    public BattleActorInput[] CreateActorInputs(QuestRun run)
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
            .Select(x => new BattleActorInput(
                ActorId: x.Id.Value,
                DisplayName: x.EnemyDefinitionId.ToString(),
                Side: BattleSide.Enemy,
                BaseStatus: new server.domain.player.Status(
                    maxHp: Math.Max(1, x.CurrentHp),
                    maxMp: Math.Max(0, x.CurrentMp),
                    strength: 1,
                    defense: 1,
                    intelligence: 1,
                    luck: 1,
                    speed: 1),
                MoveSet: new server.domain.player.MoveSet(),
                CurrentHp: x.CurrentHp,
                CurrentMp: x.CurrentMp,
                Ailments: x.Ailments.ToArray(),
                Buffs: x.Buffs.ToArray()))
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

        return new BattleFieldContext(partyPositions.Concat(enemyPositions).ToArray());
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

    public async Task<(BattleActionInput[] Actions, Move[] Moves)> CreateTurnInputsAsync(QuestRun run, IMoveRepository moveRepository)
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

            var action = CreateBattleAction(command, isEnemy: false, move);
            if (action is not null)
            {
                partyActions.Add(action);
            }
        }

        var enemyActions = run.BattleState.Enemies
            .Where(x => !x.IsDead)
            .Select(enemy => CreateEnemyNormalAttack(run, enemy))
            .Where(x => x is not null)
            .Cast<BattleActionInput>()
            .ToArray();

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

    private static BattleActionInput? CreateBattleAction(QuestSubmittedCommand command, bool isEnemy, Move? move = null)
    {
        return command.ActionKind switch
        {
            ActionKind.NormalAttack => new BattleActionInput(
                ActorId: isEnemy ? ResolveEnemyActorId(command.ParticipantId) : command.ParticipantId.Value,
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

    private static BattleActionInput? CreateEnemyNormalAttack(QuestRun run, QuestEnemyState enemy)
    {
        var reachableRows = QuestBattleReachability.GetReachableRows(enemy.Position.Row);
        var targetId = run.PartySnapshots
            .Join(
                run.BattleState.PartyMembers,
                snapshot => snapshot.ParticipantId,
                state => state.ParticipantId,
                (snapshot, state) => new { snapshot, state })
            .Where(x => !x.state.IsDead && !x.state.HasLeftQuest)
            .Where(x => reachableRows.Contains(x.snapshot.StartPosition.Row))
            .OrderBy(x => (int)x.snapshot.StartPosition.Row)
            .ThenBy(x => (int)x.snapshot.StartPosition.Column)
            .Select(x => x.snapshot.ParticipantId.Value)
            .FirstOrDefault();
        if (targetId == Guid.Empty)
        {
            return new BattleActionInput(
                ActorId: enemy.Id.Value,
                Kind: BattleActionKind.Wait,
                MoveId: null,
                TargetType: TargetType.Self,
                AttackRange: AttackRange.Single);
        }

        return new BattleActionInput(
            ActorId: enemy.Id.Value,
            Kind: BattleActionKind.NormalAttack,
            MoveId: null,
            TargetType: TargetType.Enemy,
            AttackRange: AttackRange.Single,
            TargetActorIds: [targetId]);
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

    private static Guid ResolveEnemyActorId(QuestParticipantId participantId)
    {
        return participantId.Value;
    }
}
