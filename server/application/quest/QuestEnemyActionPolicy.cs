using server.application.battle;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.quest;

namespace server.application.quest;

public class QuestEnemyActionPolicy
{
    private readonly BattleTargetingResolver targetingResolver = new();

    public BattleActionInput SelectAction(
        QuestRun run,
        QuestEnemyState enemy,
        QuestEnemyDefinition enemyDefinition,
        IReadOnlyList<Move> availableMoves,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition>? enemyDefinitions = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(enemyDefinition);
        ArgumentNullException.ThrowIfNull(availableMoves);

        var planningContext = CreatePlanningContext(run, enemy, enemyDefinition, enemyDefinitions);
        var usableMoves = availableMoves
            .Where(move => CanUseMove(enemy, move))
            .ToArray();

        var healAction = TryCreateHealAction(enemy, enemyDefinition, usableMoves, planningContext);
        if (healAction is not null)
        {
            return healAction;
        }

        var buffAction = TryCreateBuffAction(enemy, enemyDefinition, usableMoves, planningContext);
        if (buffAction is not null)
        {
            return buffAction;
        }

        return SelectAttackAction(enemy, enemyDefinition, usableMoves, planningContext);
    }

    private BattleActionInput SelectAttackAction(
        QuestEnemyState enemy,
        QuestEnemyDefinition enemyDefinition,
        IReadOnlyList<Move> usableMoves,
        EnemyPlanningContext planningContext)
    {
        var orderedDamageMoves = usableMoves
            .Where(IsDamageMove)
            .OrderByDescending(GetAttackRangePriority)
            .ThenBy(move => move.MpCost)
            .ThenBy(move => GetMoveDefinitionIndex(enemyDefinition, move.Id))
            .ToArray();

        var tauntingTargets = planningContext.PlayerTargets
            .Where(target => target.IsTaunting)
            .OrderBy(target => GetTauntPriority(target.Position))
            .ToArray();
        var backPriorityTargets = planningContext.PlayerTargets
            .OrderBy(target => GetBackPriority(target.Position))
            .ToArray();

        if (tauntingTargets.Length > 0)
        {
            var rearReachableTauntAction = TryCreateMoveAction(
                enemy,
                orderedDamageMoves.Where(move => CanReachBackRow(move, planningContext)).ToArray(),
                tauntingTargets,
                planningContext,
                resolvedTargets => resolvedTargets.Any(target => target.IsTaunting));
            if (rearReachableTauntAction is not null)
            {
                return rearReachableTauntAction;
            }

            var tauntAction = TryCreateMoveAction(
                enemy,
                orderedDamageMoves,
                tauntingTargets,
                planningContext,
                resolvedTargets => resolvedTargets.Any(target => target.IsTaunting));
            if (tauntAction is not null)
            {
                return tauntAction;
            }
        }

        var rearReachableAction = TryCreateMoveAction(
            enemy,
            orderedDamageMoves.Where(move => CanReachBackRow(move, planningContext)).ToArray(),
            backPriorityTargets,
            planningContext,
            resolvedTargets => resolvedTargets.Any(target => target.Position.Row == BattleRow.Back));
        if (rearReachableAction is not null)
        {
            return rearReachableAction;
        }

        var damageAction = TryCreateMoveAction(
            enemy,
            orderedDamageMoves,
            backPriorityTargets,
            planningContext,
            resolvedTargets => resolvedTargets.Count > 0);
        if (damageAction is not null)
        {
            return damageAction;
        }

        var normalAttack = TryCreateNormalAttack(enemy, planningContext, tauntingTargets, backPriorityTargets);
        if (normalAttack is not null)
        {
            return normalAttack;
        }

        return new BattleActionInput(
            ActorId: enemy.Id.Value,
            Kind: BattleActionKind.Wait,
            MoveId: null,
            TargetType: TargetType.Self,
            AttackRange: AttackRange.Single);
    }

    private BattleActionInput? TryCreateHealAction(
        QuestEnemyState enemy,
        QuestEnemyDefinition enemyDefinition,
        IReadOnlyList<Move> usableMoves,
        EnemyPlanningContext planningContext)
    {
        var healMoves = usableMoves.Where(IsHealMove).ToArray();
        if (healMoves.Length == 0)
        {
            return null;
        }

        var resurrectionMoves = healMoves
            .Where(move => move.TargetLifeState == TargetLifeState.Dead)
            .ToArray();
        if (resurrectionMoves.Length > 0)
        {
            foreach (var target in planningContext.FriendlyTargets
                         .Where(candidate => candidate.CurrentHp <= 0)
                         .OrderBy(candidate => GetFrontPriority(candidate.Position)))
            {
                var applicableMoves = resurrectionMoves
                    .Where(move => CanMoveTarget(move, target, planningContext))
                    .OrderBy(move => move.MpCost)
                    .ThenBy(move => GetMoveDefinitionIndex(enemyDefinition, move.Id))
                    .ToArray();
                if (applicableMoves.Length == 0)
                {
                    continue;
                }

                return CreateActionForTarget(enemy, applicableMoves[0], target);
            }
        }

        foreach (var target in EnumerateHealTargets(planningContext))
        {
            var applicableMoves = healMoves
                .Where(move => move.TargetLifeState != TargetLifeState.Dead)
                .Where(move => CanMoveTarget(move, target, planningContext))
                .ToArray();
            if (applicableMoves.Length == 0)
            {
                continue;
            }

            var selectedMove = applicableMoves
                .OrderBy(move => Math.Abs(target.CurrentHp + EstimateHealAmount(enemyDefinition.Status, move) - target.MaxHp))
                .ThenBy(move => move.MpCost)
                .ThenBy(move => GetMoveDefinitionIndex(enemyDefinition, move.Id))
                .First();

            return CreateActionForTarget(enemy, selectedMove, target);
        }

        return null;
    }

    private BattleActionInput? TryCreateBuffAction(
        QuestEnemyState enemy,
        QuestEnemyDefinition enemyDefinition,
        IReadOnlyList<Move> usableMoves,
        EnemyPlanningContext planningContext)
    {
        if (planningContext.FriendlyTargets.Count == 0)
        {
            return null;
        }

        var buffMoves = usableMoves
            .Where(IsBuffMove)
            .OrderByDescending(GetAttackRangePriority)
            .ThenByDescending(GetBuffValue)
            .ThenBy(move => move.MpCost)
            .ThenBy(move => GetMoveDefinitionIndex(enemyDefinition, move.Id))
            .ToArray();
        if (buffMoves.Length == 0)
        {
            return null;
        }

        foreach (var move in buffMoves)
        {
            if (move.TargetType == TargetType.Self)
            {
                return CreateActionForTarget(enemy, move, planningContext.SelfTarget);
            }

            foreach (var target in planningContext.FriendlyTargets.OrderBy(candidate => GetFrontPriority(candidate.Position)))
            {
                if (!CanMoveTarget(move, target, planningContext))
                {
                    continue;
                }

                return CreateActionForTarget(enemy, move, target);
            }
        }

        return null;
    }

    private static BattleActionInput CreateActionForTarget(
        QuestEnemyState enemy,
        Move move,
        CombatTargetCandidate target)
    {
        return new BattleActionInput(
            ActorId: enemy.Id.Value,
            Kind: BattleActionKind.UseMove,
            MoveId: move.Id.Id,
            TargetType: move.TargetType,
            AttackRange: move.AttackRange,
            SelectedPosition: move.TargetType == TargetType.Self ? null : target.Position);
    }

    private BattleActionInput? TryCreateNormalAttack(
        QuestEnemyState enemy,
        EnemyPlanningContext planningContext,
        IReadOnlyList<CombatTargetCandidate> tauntingTargets,
        IReadOnlyList<CombatTargetCandidate> backPriorityTargets)
    {
        if (tauntingTargets.Count > 0)
        {
            var tauntAttack = TryCreateActionForSelector(
                enemy,
                BattleActionKind.NormalAttack,
                moveId: null,
                TargetType.Enemy,
                AttackRange.Single,
                TargetLifeState.Alive,
                tauntingTargets,
                planningContext,
                resolvedTargets => resolvedTargets.Any(target => target.IsTaunting));
            if (tauntAttack is not null)
            {
                return tauntAttack;
            }
        }

        return TryCreateActionForSelector(
            enemy,
            BattleActionKind.NormalAttack,
            moveId: null,
            TargetType.Enemy,
            AttackRange.Single,
            TargetLifeState.Alive,
            backPriorityTargets,
            planningContext,
            resolvedTargets => resolvedTargets.Count > 0);
    }

    private BattleActionInput? TryCreateMoveAction(
        QuestEnemyState enemy,
        IReadOnlyList<Move> orderedMoves,
        IReadOnlyList<CombatTargetCandidate> preferredTargets,
        EnemyPlanningContext planningContext,
        Func<IReadOnlyList<CombatTargetCandidate>, bool> predicate)
    {
        foreach (var move in orderedMoves)
        {
            var action = TryCreateActionForSelector(
                enemy,
                BattleActionKind.UseMove,
                move.Id.Id,
                move.TargetType,
                move.AttackRange,
                move.TargetLifeState,
                preferredTargets,
                planningContext,
                predicate);
            if (action is not null)
            {
                return action;
            }
        }

        return null;
    }

    private BattleActionInput? TryCreateActionForSelector(
        QuestEnemyState enemy,
        BattleActionKind kind,
        int? moveId,
        TargetType targetType,
        AttackRange attackRange,
        TargetLifeState targetLifeState,
        IReadOnlyList<CombatTargetCandidate> preferredTargets,
        EnemyPlanningContext planningContext,
        Func<IReadOnlyList<CombatTargetCandidate>, bool> predicate)
    {
        foreach (var target in preferredTargets)
        {
            var resolvedTargets = ResolveTargets(targetType, attackRange, targetLifeState, target.Position, planningContext);
            if (resolvedTargets.Count == 0 || !predicate(resolvedTargets))
            {
                continue;
            }

            return new BattleActionInput(
                ActorId: enemy.Id.Value,
                Kind: kind,
                MoveId: moveId,
                TargetType: targetType,
                AttackRange: attackRange,
                SelectedPosition: target.Position);
        }

        return null;
    }

    private bool CanReachBackRow(Move move, EnemyPlanningContext planningContext)
    {
        foreach (var target in planningContext.PlayerTargets)
        {
            var resolvedTargets = ResolveTargets(move.TargetType, move.AttackRange, move.TargetLifeState, target.Position, planningContext);
            if (resolvedTargets.Any(resolvedTarget => resolvedTarget.Position.Row == BattleRow.Back))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanMoveTarget(
        Move move,
        CombatTargetCandidate target,
        EnemyPlanningContext planningContext)
    {
        if (move.TargetType == TargetType.Self)
        {
            return target.ActorId == planningContext.ActorSnapshot.Id;
        }

        if (move.TargetType == TargetType.Enemy || move.TargetType == TargetType.Ally)
        {
            var resolvedTargets = ResolveTargets(move.TargetType, move.AttackRange, move.TargetLifeState, target.Position, planningContext);
            return resolvedTargets.Any(resolved => resolved.ActorId == target.ActorId);
        }

        return false;
    }

    private IReadOnlyList<CombatTargetCandidate> ResolveTargets(
        TargetType targetType,
        AttackRange attackRange,
        TargetLifeState targetLifeState,
        BattlePosition selectedPosition,
        EnemyPlanningContext planningContext)
    {
        var selector = new BattleTargetSelector(targetType, attackRange, selectedPosition: selectedPosition, targetLifeState: targetLifeState);
        var targetIds = targetingResolver.ResolveTargets(
            selector,
            planningContext.ActorSnapshot,
            planningContext.Snapshots,
            planningContext.States,
            planningContext.FieldContext);
        if (targetIds.Count == 0)
        {
            return [];
        }

        var targetIdSet = targetIds.ToHashSet();
        return planningContext.CandidatesByActorId
            .Where(pair => targetIdSet.Contains(pair.Key))
            .Select(pair => pair.Value)
            .ToArray();
    }

    private static IEnumerable<CombatTargetCandidate> EnumerateHealTargets(EnemyPlanningContext planningContext)
    {
        if (planningContext.SelfTarget.CurrentHp <= planningContext.SelfTarget.MaxHp / 2)
        {
            yield return planningContext.SelfTarget;
        }

        foreach (var target in planningContext.FriendlyTargets.OrderBy(candidate => GetFrontPriority(candidate.Position)))
        {
            if (target.CurrentHp <= target.MaxHp / 2)
            {
                yield return target;
            }
        }
    }

    private static EnemyPlanningContext CreatePlanningContext(
        QuestRun run,
        QuestEnemyState enemy,
        QuestEnemyDefinition enemyDefinition,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition>? enemyDefinitions)
    {
        var actorSnapshot = new BattleActorSnapshot(
            new BattleActorId(enemy.Id.Value),
            enemyDefinition.Name,
            BattleSide.Enemy,
            enemyDefinition.Status,
            CreateMoveSet(enemyDefinition.MoveIds));
        var actorState = new BattleActorState(
            actorSnapshot.Id,
            enemy.CurrentHp,
            enemy.CurrentMp,
            enemy.Ailments,
            enemy.Buffs);
        var selfTarget = new CombatTargetCandidate(
            actorSnapshot.Id,
            enemy.Position,
            enemyDefinition.Status.MaxHp,
            enemy.CurrentHp,
            IsTaunting: false,
            new BattleActorSnapshot(actorSnapshot.Id, enemyDefinition.Name, BattleSide.Enemy, enemyDefinition.Status, CreateMoveSet(enemyDefinition.MoveIds)),
            actorState);

        var playerTargets = run.PartySnapshots
            .Join(
                run.BattleState.PartyMembers,
                snapshot => snapshot.ParticipantId,
                state => state.ParticipantId,
                (snapshot, state) => new { snapshot, state })
            .Where(x => !x.state.IsDead && !x.state.HasLeftQuest)
            .Select(x => new CombatTargetCandidate(
                new BattleActorId(x.snapshot.ParticipantId.Value),
                x.snapshot.StartPosition,
                x.snapshot.BaseStatus.MaxHp,
                x.state.CurrentHp,
                x.state.Ailments.Any(ailment => ailment.Type == AilmentType.Taunt),
                new BattleActorSnapshot(
                    new BattleActorId(x.snapshot.ParticipantId.Value),
                    x.snapshot.DisplayName,
                    BattleSide.Ally,
                    x.snapshot.BaseStatus,
                    x.snapshot.MoveSet),
                new BattleActorState(
                    new BattleActorId(x.snapshot.ParticipantId.Value),
                    x.state.CurrentHp,
                    x.state.CurrentMp,
                    x.state.Ailments,
                    x.state.Buffs)))
            .ToArray();

        var friendlyTargets = run.BattleState.Enemies
            .Where(other => other.Id != enemy.Id)
            .Select(other =>
            {
                var otherDefinition = ResolveDefinition(other.EnemyDefinitionId, enemyDefinitions);
                var actorId = new BattleActorId(other.Id.Value);
                return new CombatTargetCandidate(
                    actorId,
                    other.Position,
                    otherDefinition.Status.MaxHp,
                    other.CurrentHp,
                    IsTaunting: false,
                    new BattleActorSnapshot(actorId, otherDefinition.Name, BattleSide.Enemy, otherDefinition.Status, CreateMoveSet(otherDefinition.MoveIds)),
                    new BattleActorState(actorId, other.CurrentHp, other.CurrentMp, other.Ailments, other.Buffs));
            })
            .ToArray();

        var allTargets = playerTargets.Concat(friendlyTargets).Append(selfTarget).ToArray();
        var snapshots = allTargets.Select(target => target.Snapshot).Append(actorSnapshot).DistinctBy(x => x.Id).ToArray();
        var states = allTargets.Select(target => target.State).Append(actorState).DistinctBy(x => x.Id).ToArray();
        var positions = allTargets
            .Select(target => new BattleActorPosition(target.ActorId, target.Position))
            .Append(new BattleActorPosition(actorSnapshot.Id, enemy.Position))
            .DistinctBy(x => x.ActorId)
            .ToArray();

        return new EnemyPlanningContext(
            actorSnapshot,
            selfTarget,
            playerTargets,
            friendlyTargets,
            allTargets.ToDictionary(target => target.ActorId),
            snapshots,
            states,
            new BattleFieldContext(positions));
    }

    private static QuestEnemyDefinition ResolveDefinition(
        QuestEnemyDefinitionId definitionId,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition>? enemyDefinitions)
    {
        if (enemyDefinitions is not null && enemyDefinitions.TryGetValue(definitionId, out var definition))
        {
            return definition;
        }

        return new QuestEnemyDefinition(
            definitionId,
            $"Enemy-{definitionId.Value}",
            1,
            new Status(1, 0, 1, 1, 1, 1, 1),
            "/image/battle/placeholder.png",
            server.domain.quest.enums.EnemyAiType.Aggressive,
            []);
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

    private static bool CanUseMove(QuestEnemyState enemy, Move move)
    {
        return enemy.CurrentMp >= move.MpCost;
    }

    private static bool IsDamageMove(Move move)
    {
        return move.TargetType == TargetType.Enemy &&
               move.Effects.Any(effect => effect.EffectType == MoveEffectType.Damage);
    }

    private static bool IsHealMove(Move move)
    {
        return move.Effects.Any(effect => effect.EffectType == MoveEffectType.Heal);
    }

    private static bool IsBuffMove(Move move)
    {
        return move.TargetType is TargetType.Ally or TargetType.Self &&
               move.Effects.Any(effect => effect.EffectType == MoveEffectType.Buff);
    }

    private static int EstimateHealAmount(Status healerStatus, Move move)
    {
        var effect = move.Effects.First(x => x.EffectType == MoveEffectType.Heal);
        var damage = effect.Damage ?? throw new InvalidOperationException("Heal 効果に Damage 定義がありません。");
        var attackStat = damage.AttackStat ?? (move.Category != MoveCategory.Attack ? BuffStat.Intelligence : BuffStat.Strength);
        var attackPower = attackStat switch
        {
            BuffStat.Intelligence => healerStatus.Intelligence,
            BuffStat.Defense => healerStatus.Defense,
            _ => healerStatus.Strength
        };

        return Math.Max(1, damage.FixedValue + (int)Math.Round(attackPower * damage.PowerRate, MidpointRounding.AwayFromZero));
    }

    private static decimal GetBuffValue(Move move)
    {
        return move.Effects
            .Where(effect => effect.EffectType == MoveEffectType.Buff && effect.Buff is not null)
            .Select(effect => effect.Buff!.BuffValue)
            .DefaultIfEmpty(0m)
            .Max();
    }

    private static int GetAttackRangePriority(Move move)
    {
        return move.AttackRange switch
        {
            AttackRange.All => 5,
            AttackRange.Square => 4,
            AttackRange.Row => 3,
            AttackRange.Column => 3,
            AttackRange.Single => 2,
            _ => 0
        };
    }

    private static int GetMoveDefinitionIndex(QuestEnemyDefinition definition, MoveId moveId)
    {
        for (var i = 0; i < definition.MoveIds.Count; i++)
        {
            if (definition.MoveIds[i].Id == moveId.Id)
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static int GetBackPriority(BattlePosition position)
    {
        return position.Row switch
        {
            BattleRow.Back when position.Column == BattleColumn.Left => 0,
            BattleRow.Back when position.Column == BattleColumn.Right => 1,
            BattleRow.Middle when position.Column == BattleColumn.Left => 2,
            BattleRow.Middle when position.Column == BattleColumn.Right => 3,
            BattleRow.Front when position.Column == BattleColumn.Left => 4,
            BattleRow.Front when position.Column == BattleColumn.Right => 5,
            _ => int.MaxValue
        };
    }

    private static int GetFrontPriority(BattlePosition position)
    {
        return position.Row switch
        {
            BattleRow.Front when position.Column == BattleColumn.Left => 0,
            BattleRow.Front when position.Column == BattleColumn.Right => 1,
            BattleRow.Middle when position.Column == BattleColumn.Left => 2,
            BattleRow.Middle when position.Column == BattleColumn.Right => 3,
            BattleRow.Back when position.Column == BattleColumn.Left => 4,
            BattleRow.Back when position.Column == BattleColumn.Right => 5,
            _ => int.MaxValue
        };
    }

    private static int GetTauntPriority(BattlePosition position)
    {
        return GetFrontPriority(position);
    }

    private sealed record EnemyPlanningContext(
        BattleActorSnapshot ActorSnapshot,
        CombatTargetCandidate SelfTarget,
        IReadOnlyList<CombatTargetCandidate> PlayerTargets,
        IReadOnlyList<CombatTargetCandidate> FriendlyTargets,
        IReadOnlyDictionary<BattleActorId, CombatTargetCandidate> CandidatesByActorId,
        IReadOnlyList<BattleActorSnapshot> Snapshots,
        IReadOnlyList<BattleActorState> States,
        BattleFieldContext FieldContext);

    private sealed record CombatTargetCandidate(
        BattleActorId ActorId,
        BattlePosition Position,
        int MaxHp,
        int CurrentHp,
        bool IsTaunting,
        BattleActorSnapshot Snapshot,
        BattleActorState State);
}
