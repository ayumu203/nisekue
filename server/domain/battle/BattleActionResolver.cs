using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;

namespace server.domain.battle;

public class BattleActionResolver(
    BattleDamageCalculator battleDamageCalculator,
    BattleStatusResolver battleStatusResolver)
{
    public BattleActionResult Resolve(
        BattleAction action,
        IEnumerable<BattleActorSnapshot> snapshots,
        IEnumerable<BattleActorState> states,
        IEnumerable<Move> moves)
    {
        ArgumentNullException.ThrowIfNull(action);
        var snapshotMap = snapshots?.ToDictionary(x => x.Id) ?? throw new ArgumentNullException(nameof(snapshots));
        var stateMap = states?.ToDictionary(x => x.Id) ?? throw new ArgumentNullException(nameof(states));
        var moveMap = moves?.ToDictionary(x => x.Id.Id) ?? throw new ArgumentNullException(nameof(moves));

        if (!snapshotMap.TryGetValue(action.ActorId, out var actorSnapshot) ||
            !stateMap.TryGetValue(action.ActorId, out var actorState) ||
            actorState.IsDead)
        {
            return new BattleActionResult(action.ActorId, false);
        }

        if (!actorState.CanAct())
        {
            return new BattleActionResult(action.ActorId, false);
        }

        if (ShouldSkipActionByParalysis(actorState))
        {
            return new BattleActionResult(action.ActorId, false);
        }

        return action.Kind switch
        {
            BattleActionKind.NormalAttack => ResolveNormalAttack(action, actorSnapshot, actorState, snapshotMap, stateMap),
            BattleActionKind.UseMove => ResolveMove(action, actorSnapshot, actorState, snapshotMap, stateMap, moveMap),
            BattleActionKind.Guard => ResolveGuard(actorSnapshot, actorState),
            BattleActionKind.Wait => new BattleActionResult(action.ActorId, true),
            _ => throw new ArgumentOutOfRangeException(nameof(action.Kind), $"未対応の BattleActionKind: {action.Kind}")
        };
    }

    private BattleActionResult ResolveNormalAttack(
        BattleAction action,
        BattleActorSnapshot actorSnapshot,
        BattleActorState actorState,
        IReadOnlyDictionary<BattleActorId, BattleActorSnapshot> snapshotMap,
        IReadOnlyDictionary<BattleActorId, BattleActorState> stateMap)
    {
        var targets = ResolveTargets(action.Target, actorSnapshot, snapshotMap, stateMap);
        if (targets.Count == 0)
        {
            return new BattleActionResult(action.ActorId, false);
        }

        var attackerStatus = battleStatusResolver.BuildEffectiveStatus(actorSnapshot, actorState);
        var usesIntelligence = attackerStatus.Intelligence > attackerStatus.Strength;
        var targetResults = new List<BattleTargetResult>();

        foreach (var targetId in targets)
        {
            var targetSnapshot = snapshotMap[targetId];
            var targetState = stateMap[targetId];
            if (targetState.IsDead)
            {
                continue;
            }

            var defenderStatus = battleStatusResolver.BuildEffectiveStatus(targetSnapshot, targetState);
            var damageResult = battleDamageCalculator.Calculate(new BattleDamageInput(
                actorSnapshot.Id,
                targetId,
                attackerStatus,
                defenderStatus,
                fixedPower: 0,
                powerRate: 1m,
                criticalRate: 0m,
                elementType: ElementType.None,
                usesIntelligence: usesIntelligence));

            targetState.ReceiveDamage(damageResult.Damage);
            targetResults.Add(new BattleTargetResult(targetId, damageResult.Damage, targetState.IsDead, null));
        }

        return new BattleActionResult(action.ActorId, targetResults.Count > 0, targetResults);
    }

    private BattleActionResult ResolveMove(
        BattleAction action,
        BattleActorSnapshot actorSnapshot,
        BattleActorState actorState,
        IReadOnlyDictionary<BattleActorId, BattleActorSnapshot> snapshotMap,
        IReadOnlyDictionary<BattleActorId, BattleActorState> stateMap,
        IReadOnlyDictionary<int, Move> moveMap)
    {
        if (action.MoveId is null || !moveMap.TryGetValue(action.MoveId.Id, out var move))
        {
            return new BattleActionResult(action.ActorId, false);
        }

        if (!actorSnapshot.MoveSet.GetLearnedMoveIds().Any(x => x.Id == move.Id.Id))
        {
            return new BattleActionResult(action.ActorId, false);
        }

        if (actorState.CurrentMp < move.MpCost)
        {
            return new BattleActionResult(action.ActorId, false);
        }

        var targets = ResolveTargets(
            new BattleTargetSelector(move.TargetType, move.AttackRange, action.Target.TargetActorIds),
            actorSnapshot,
            snapshotMap,
            stateMap);
        if (targets.Count == 0)
        {
            return new BattleActionResult(action.ActorId, false);
        }

        actorState.ConsumeMp(move.MpCost);

        var attackerStatus = battleStatusResolver.BuildEffectiveStatus(actorSnapshot, actorState);
        var usesIntelligence = ShouldUseIntelligence(move, attackerStatus);
        var targetResults = new List<BattleTargetResult>();

        foreach (var effect in move.GetOrderedEffects())
        {
            foreach (var targetId in targets)
            {
                var targetSnapshot = snapshotMap[targetId];
                var targetState = stateMap[targetId];
                if (targetState.IsDead && effect.EffectType != MoveEffectType.Heal)
                {
                    continue;
                }

                var defenderStatus = battleStatusResolver.BuildEffectiveStatus(targetSnapshot, targetState);
                var result = ResolveEffect(
                    actorSnapshot,
                    attackerStatus,
                    targetSnapshot,
                    defenderStatus,
                    targetState,
                    effect,
                    usesIntelligence);
                if (result is not null)
                {
                    targetResults.Add(result);
                }
            }
        }

        return new BattleActionResult(action.ActorId, true, targetResults);
    }

    private BattleActionResult ResolveGuard(BattleActorSnapshot actorSnapshot, BattleActorState actorState)
    {
        actorState.ApplyBuff(new BattleBuffState(BuffStat.Defense, BuffCalculationType.Mul, 1.5m, 1), canStack: false);
        actorState.ApplyBuff(new BattleBuffState(BuffStat.Intelligence, BuffCalculationType.Mul, 1.5m, 1), canStack: false);
        return new BattleActionResult(actorSnapshot.Id, true);
    }

    private BattleTargetResult? ResolveEffect(
        BattleActorSnapshot actorSnapshot,
        Status attackerStatus,
        BattleActorSnapshot targetSnapshot,
        Status defenderStatus,
        BattleActorState targetState,
        MoveEffect effect,
        bool usesIntelligence)
    {
        return effect.EffectType switch
        {
            MoveEffectType.Damage => ResolveDamageEffect(actorSnapshot, attackerStatus, targetSnapshot, defenderStatus, targetState, effect, usesIntelligence),
            MoveEffectType.Heal => ResolveHealEffect(attackerStatus, targetSnapshot, defenderStatus, targetState, effect, usesIntelligence),
            MoveEffectType.Ailment => ResolveAilmentEffect(actorSnapshot, attackerStatus, defenderStatus, targetState, effect),
            MoveEffectType.Buff => ResolveBuffEffect(actorSnapshot, attackerStatus, defenderStatus, targetState, effect),
            _ => throw new ArgumentOutOfRangeException(nameof(effect.EffectType), $"未対応の MoveEffectType: {effect.EffectType}")
        };
    }

    private BattleTargetResult ResolveDamageEffect(
        BattleActorSnapshot actorSnapshot,
        Status attackerStatus,
        BattleActorSnapshot targetSnapshot,
        Status defenderStatus,
        BattleActorState targetState,
        MoveEffect effect,
        bool usesIntelligence)
    {
        ArgumentNullException.ThrowIfNull(effect.Damage);

        var totalDamage = 0;
        for (var i = 0; i < effect.Damage.HitCount; i++)
        {
            var damageResult = battleDamageCalculator.Calculate(new BattleDamageInput(
                actorSnapshot.Id,
                targetSnapshot.Id,
                attackerStatus,
                defenderStatus,
                effect.Damage.FixedValue,
                effect.Damage.PowerRate,
                effect.Damage.CriticalRate,
                effect.Damage.ElementType,
                usesIntelligence));
            totalDamage += damageResult.Damage;
        }

        targetState.ReceiveDamage(totalDamage);
        return new BattleTargetResult(targetSnapshot.Id, totalDamage, targetState.IsDead, null);
    }

    private static BattleTargetResult ResolveHealEffect(
        Status attackerStatus,
        BattleActorSnapshot targetSnapshot,
        Status defenderStatus,
        BattleActorState targetState,
        MoveEffect effect,
        bool usesIntelligence)
    {
        ArgumentNullException.ThrowIfNull(effect.Damage);

        var attackPower = usesIntelligence ? attackerStatus.Intelligence : attackerStatus.Strength;
        var healValue = effect.Damage.FixedValue + (int)Math.Round(attackPower * effect.Damage.PowerRate, MidpointRounding.AwayFromZero);
        targetState.RestoreHp(Math.Max(1, healValue), defenderStatus.MaxHp);

        return new BattleTargetResult(targetSnapshot.Id, 0, false, null);
    }

    private static BattleTargetResult ResolveAilmentEffect(
        BattleActorSnapshot actorSnapshot,
        Status attackerStatus,
        Status defenderStatus,
        BattleActorState targetState,
        MoveEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect.Ailment);

        var appliedAilment = default(AilmentType?);
        if (ShouldApplySecondaryEffect(effect.Ailment.AilmentRate, attackerStatus, defenderStatus))
        {
            targetState.ApplyAilment(new BattleAilmentState(effect.Ailment.AilmentType, 1));
            appliedAilment = effect.Ailment.AilmentType;
        }

        return new BattleTargetResult(targetState.Id, 0, targetState.IsDead, appliedAilment);
    }

    private static BattleTargetResult ResolveBuffEffect(
        BattleActorSnapshot actorSnapshot,
        Status attackerStatus,
        Status defenderStatus,
        BattleActorState targetState,
        MoveEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect.Buff);

        if (ShouldApplySecondaryEffect(effect.Buff.BuffRate, attackerStatus, defenderStatus))
        {
            targetState.ApplyBuff(
                new BattleBuffState(effect.Buff.BuffStat, effect.Buff.BuffCalculationType, effect.Buff.BuffValue, effect.Buff.BuffTurns),
                effect.Buff.CanStack);
        }

        return new BattleTargetResult(targetState.Id, 0, targetState.IsDead, null);
    }

    private static bool ShouldUseIntelligence(Move move, Status attackerStatus)
    {
        return move.Category != MoveCategory.Attack || attackerStatus.Intelligence > attackerStatus.Strength;
    }

    private static bool ShouldApplySecondaryEffect(decimal rate, Status attackerStatus, Status defenderStatus)
    {
        return rate >= 1m || (rate > 0m && attackerStatus.Luck >= defenderStatus.Luck);
    }

    private static bool ShouldSkipActionByParalysis(BattleActorState actorState)
    {
        if (!actorState.Ailments.Any(x => x.Type == AilmentType.Paralysis))
        {
            return false;
        }

        return Random.Shared.NextDouble() < 0.3d;
    }

    private static IReadOnlyList<BattleActorId> ResolveTargets(
        BattleTargetSelector selector,
        BattleActorSnapshot actorSnapshot,
        IReadOnlyDictionary<BattleActorId, BattleActorSnapshot> snapshotMap,
        IReadOnlyDictionary<BattleActorId, BattleActorState> stateMap)
    {
        var candidates = snapshotMap.Values
            .Where(x => IsTargetTypeMatch(selector.TargetType, actorSnapshot, x))
            .Where(x => stateMap.TryGetValue(x.Id, out var state) && !state.IsDead)
            .OrderBy(x => x.Id.Value)
            .Select(x => x.Id)
            .ToArray();

        if (selector.TargetActorIds.Count > 0)
        {
            candidates = candidates
                .Where(x => selector.TargetActorIds.Contains(x))
                .ToArray();
        }

        return selector.AttackRange switch
        {
            AttackRange.Single => candidates.Take(1).ToArray(),
            AttackRange.Column => candidates.Take(2).ToArray(),
            AttackRange.Row => candidates.Take(2).ToArray(),
            AttackRange.Square => candidates.Take(4).ToArray(),
            AttackRange.All => candidates,
            _ => throw new ArgumentOutOfRangeException(nameof(selector.AttackRange), $"未対応の AttackRange: {selector.AttackRange}")
        };
    }

    private static bool IsTargetTypeMatch(TargetType targetType, BattleActorSnapshot actorSnapshot, BattleActorSnapshot targetSnapshot)
    {
        return targetType switch
        {
            TargetType.Enemy => actorSnapshot.Side != targetSnapshot.Side,
            TargetType.Ally => actorSnapshot.Side == targetSnapshot.Side && actorSnapshot.Id != targetSnapshot.Id,
            TargetType.Self => actorSnapshot.Id == targetSnapshot.Id,
            _ => throw new ArgumentOutOfRangeException(nameof(targetType), $"未対応の TargetType: {targetType}")
        };
    }
}
