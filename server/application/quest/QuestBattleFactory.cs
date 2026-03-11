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
        var moveIds = new HashSet<int>();

        foreach (var member in run.BattleState.PartyMembers)
        {
            if (member.IsDead || member.HasLeftQuest || member.CanActFromTurn > run.TurnState.CurrentTurnNo)
            {
                continue;
            }

            var command = pending.TryGetValue(member.ParticipantId, out var submitted)
                ? submitted
                : CreateFallbackPartyCommand(run, member);

            var action = CreateBattleAction(run, command, isEnemy: false);
            if (action is not null)
            {
                partyActions.Add(action);
                if (action.MoveId is not null)
                {
                    moveIds.Add(action.MoveId.Value);
                }
            }
        }

        var enemyActions = run.BattleState.Enemies
            .Where(x => !x.IsDead)
            .Select(enemy => CreateEnemyNormalAttack(run, enemy))
            .Where(x => x is not null)
            .Cast<BattleActionInput>()
            .ToArray();

        var moves = new List<Move>();
        foreach (var moveId in moveIds)
        {
            var move = await moveRepository.GetMoveAsync(new MoveId(moveId))
                ?? throw new KeyNotFoundException($"技定義が見つかりません。 moveId={moveId}");
            moves.Add(move);
        }

        return (partyActions.Concat(enemyActions).ToArray(), moves.ToArray());
    }

    private static QuestSubmittedCommand CreateFallbackPartyCommand(QuestRun run, QuestRunPartyMemberState member)
    {
        var selectedTargetPosition = run.BattleState.Enemies
            .Where(x => !x.IsDead)
            .OrderBy(x => (int)x.Position.Row)
            .ThenBy(x => (int)x.Position.Column)
            .Select(x => (BattlePosition?)x.Position)
            .FirstOrDefault();

        return new QuestSubmittedCommand(
            member.ParticipantId,
            run.TurnState.CurrentTurnNo,
            ActionKind.NormalAttack,
            DateTimeOffset.UtcNow,
            selectedTargetPosition: selectedTargetPosition,
            isAutoSubmitted: true);
    }

    private static BattleActionInput? CreateBattleAction(QuestRun run, QuestSubmittedCommand command, bool isEnemy)
    {
        return command.ActionKind switch
        {
            ActionKind.NormalAttack => new BattleActionInput(
                ActorId: isEnemy ? ResolveEnemyActorId(command.ParticipantId) : command.ParticipantId.Value,
                Kind: BattleActionKind.NormalAttack,
                MoveId: null,
                TargetType: TargetType.Enemy,
                AttackRange: AttackRange.Single,
                TargetActorIds: ResolveTargetActorIds(run, command.SelectedTargetPosition, targetEnemy: !isEnemy)),
            ActionKind.UseMove when command.MoveId is not null => new BattleActionInput(
                ActorId: command.ParticipantId.Value,
                Kind: BattleActionKind.UseMove,
                MoveId: command.MoveId.Id,
                TargetType: TargetType.Enemy,
                AttackRange: AttackRange.Single,
                TargetActorIds: ResolveTargetActorIds(run, command.SelectedTargetPosition, targetEnemy: true)),
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
        var targetId = run.PartySnapshots
            .Join(
                run.BattleState.PartyMembers,
                snapshot => snapshot.ParticipantId,
                state => state.ParticipantId,
                (snapshot, state) => new { snapshot, state })
            .Where(x => !x.state.IsDead && !x.state.HasLeftQuest)
            .OrderBy(x => (int)x.snapshot.StartPosition.Row)
            .ThenBy(x => (int)x.snapshot.StartPosition.Column)
            .Select(x => x.snapshot.ParticipantId.Value)
            .FirstOrDefault();
        if (targetId == Guid.Empty)
        {
            return null;
        }

        return new BattleActionInput(
            ActorId: enemy.Id.Value,
            Kind: BattleActionKind.NormalAttack,
            MoveId: null,
            TargetType: TargetType.Enemy,
            AttackRange: AttackRange.Single,
            TargetActorIds: [targetId]);
    }

    private static IReadOnlyList<Guid>? ResolveTargetActorIds(QuestRun run, BattlePosition? position, bool targetEnemy)
    {
        if (position is null)
        {
            var fallback = targetEnemy
                ? run.BattleState.Enemies.FirstOrDefault(x => !x.IsDead)?.Id.Value
                : run.PartySnapshots
                    .Join(run.BattleState.PartyMembers, x => x.ParticipantId, x => x.ParticipantId, (snapshot, state) => new { snapshot, state })
                    .FirstOrDefault(x => !x.state.IsDead && !x.state.HasLeftQuest)?.snapshot.ParticipantId.Value;
            return fallback is null ? null : [fallback.Value];
        }

        if (targetEnemy)
        {
            var enemyId = run.BattleState.Enemies
                .FirstOrDefault(x => !x.IsDead && x.Position == position.Value)
                ?.Id.Value;
            return enemyId is null ? null : [enemyId.Value];
        }

        var participantId = run.PartySnapshots
            .Join(run.BattleState.PartyMembers, x => x.ParticipantId, x => x.ParticipantId, (snapshot, state) => new { snapshot, state })
            .FirstOrDefault(x => !x.state.IsDead && !x.state.HasLeftQuest && x.snapshot.StartPosition == position.Value)
            ?.snapshot.ParticipantId.Value;
        return participantId is null ? null : [participantId.Value];
    }

    private static Guid ResolveEnemyActorId(QuestParticipantId participantId)
    {
        return participantId.Value;
    }
}
