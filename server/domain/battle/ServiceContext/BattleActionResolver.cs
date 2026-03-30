using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;

namespace server.domain.battle;

public class BattleActionResolver(
    BattleDamageCalculator battleDamageCalculator,
    BattleStatusResolver battleStatusResolver,
    BattleTargetingResolver battleTargetingResolver,
    Func<double>? randomProvider = null)
{
    private readonly Func<double> _randomProvider = randomProvider ?? Random.Shared.NextDouble;

    public BattleActionResult Resolve(
        BattleAction action,
        IEnumerable<BattleActorSnapshot> snapshots,
        IEnumerable<BattleActorState> states,
        IEnumerable<Move> moves,
        BattleFieldContext? fieldContext = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        var snapshotMap = snapshots?.ToDictionary(x => x.Id) ?? throw new ArgumentNullException(nameof(snapshots));
        var stateMap = states?.ToDictionary(x => x.Id) ?? throw new ArgumentNullException(nameof(states));
        var moveMap = moves?.ToDictionary(x => x.Id.Id) ?? throw new ArgumentNullException(nameof(moves));

        if (!snapshotMap.TryGetValue(action.ActorId, out var actorSnapshot) ||
            !stateMap.TryGetValue(action.ActorId, out var actorState))
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.ActorUnavailable);
        }

        if (actorState.IsDead)
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.ActorUnavailable);
        }

        if (!actorState.CanAct())
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.CannotAct);
        }

        if (actorState.Ailments.Any(x => x.Type == AilmentType.Sleep))
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.Sleeping);
        }

        if (ShouldSkipActionByParalysis(actorState))
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.Paralyzed);
        }

        return action.Kind switch
        {
            BattleActionKind.NormalAttack => ResolveNormalAttack(action, actorSnapshot, actorState, snapshotMap, stateMap, fieldContext),
            BattleActionKind.UseMove => ResolveMove(action, actorSnapshot, actorState, snapshotMap, stateMap, moveMap, fieldContext),
            BattleActionKind.Prayer => ResolvePrayer(action, actorSnapshot, snapshotMap, stateMap, fieldContext),
            BattleActionKind.Guard => ResolveGuard(action, actorSnapshot, actorState),
            BattleActionKind.Wait => new BattleActionResult(action.ActorId, action.Kind, action.MoveId, true),
            _ => throw new ArgumentOutOfRangeException(nameof(action.Kind), $"未対応の BattleActionKind: {action.Kind}")
        };
    }

    private BattleActionResult ResolveNormalAttack(
        BattleAction action,
        BattleActorSnapshot actorSnapshot,
        BattleActorState actorState,
        IReadOnlyDictionary<BattleActorId, BattleActorSnapshot> snapshotMap,
        IReadOnlyDictionary<BattleActorId, BattleActorState> stateMap,
        BattleFieldContext? fieldContext)
    {
        var targets = battleTargetingResolver.ResolveTargets(action.Target, actorSnapshot, snapshotMap.Values, stateMap.Values, fieldContext);
        if (targets.Count == 0)
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.NoTarget);
        }

        var attackerStatus = battleStatusResolver.BuildEffectiveStatus(actorSnapshot, actorState);
        var targetResults = new List<BattleTargetResult>();

        foreach (var targetId in targets)
        {
            var targetSnapshot = snapshotMap[targetId];
            var targetState = stateMap[targetId];
            if (targetState.IsDead)
            {
                continue;
            }

            if (!ShouldHit(attackerStatus, TargetType.Enemy))
            {
                targetResults.Add(new BattleTargetResult(targetId, 0, 0, 0, targetState.IsDead, null));
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
                attackStat: BuffStat.Strength));

            targetState.ReceiveDamage(damageResult.Damage);
            targetResults.Add(new BattleTargetResult(targetId, damageResult.Damage, -damageResult.Damage, 0, targetState.IsDead, null));
        }

        return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, targetResults.Count > 0, targetResults: targetResults);
    }

    private BattleActionResult ResolveMove(
        BattleAction action,
        BattleActorSnapshot actorSnapshot,
        BattleActorState actorState,
        IReadOnlyDictionary<BattleActorId, BattleActorSnapshot> snapshotMap,
        IReadOnlyDictionary<BattleActorId, BattleActorState> stateMap,
        IReadOnlyDictionary<int, Move> moveMap,
        BattleFieldContext? fieldContext)
    {
        if (action.MoveId is null || !moveMap.TryGetValue(action.MoveId.Id, out var move))
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.MoveUnavailable);
        }

        if (!actorSnapshot.MoveSet.GetLearnedMoveIds().Any(x => x.Id == move.Id.Id))
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.MoveUnavailable);
        }

        if (actorState.CurrentMp < move.MpCost)
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.InsufficientMp);
        }

        var targets = ResolveTargets(
            new BattleTargetSelector(move.TargetType, move.AttackRange, action.Target.TargetActorIds, action.Target.SelectedPosition, move.TargetLifeState),
            actorSnapshot,
            snapshotMap.Values,
            stateMap.Values,
            fieldContext);
        if (targets.Count == 0)
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.NoTarget);
        }

        actorState.ConsumeMp(move.MpCost);

        var attackerStatus = battleStatusResolver.BuildEffectiveStatus(actorSnapshot, actorState);
        var targetResults = new List<BattleTargetResult>();

        foreach (var effect in move.GetOrderedEffects())
        {
            var effectTargets = ResolveEffectTargets(
                action,
                move,
                effect,
                actorSnapshot,
                snapshotMap.Values,
                stateMap.Values,
                fieldContext,
                targets);

            foreach (var targetId in effectTargets)
            {
                var targetSnapshot = snapshotMap[targetId];
                var targetState = stateMap[targetId];
                if (targetState.IsDead && effect.EffectType != MoveEffectType.Heal && effect.EffectType != MoveEffectType.RestoreMp)
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
                    move,
                    effect,
                    move.Category != MoveCategory.Attack);
                if (result is not null)
                {
                    targetResults.Add(result);
                }
            }
        }

        return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, true, targetResults: targetResults);
    }

    private BattleActionResult ResolveGuard(BattleAction action, BattleActorSnapshot actorSnapshot, BattleActorState actorState)
    {
        actorState.ApplyBuff(new BattleBuffState(BuffStat.Defense, BuffCalculationType.Mul, 1.5m, 1), canStack: false);
        actorState.ApplyBuff(new BattleBuffState(BuffStat.Intelligence, BuffCalculationType.Mul, 1.5m, 1), canStack: false);
        return new BattleActionResult(actorSnapshot.Id, action.Kind, action.MoveId, true);
    }

    private BattleActionResult ResolvePrayer(
        BattleAction action,
        BattleActorSnapshot actorSnapshot,
        IReadOnlyDictionary<BattleActorId, BattleActorSnapshot> snapshotMap,
        IReadOnlyDictionary<BattleActorId, BattleActorState> stateMap,
        BattleFieldContext? fieldContext)
    {
        var targets = ResolveTargets(action.Target, actorSnapshot, snapshotMap.Values, stateMap.Values, fieldContext);
        if (targets.Count == 0)
        {
            return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, false, BattleActionFailureReason.NoTarget);
        }

        var targetResults = new List<BattleTargetResult>();
        foreach (var targetId in targets)
        {
            var targetState = stateMap[targetId];
            if (targetState.IsDead)
            {
                continue;
            }

            targetState.ApplyBuff(new BattleBuffState(BuffStat.Strength, BuffCalculationType.Mul, 1.5m, 1), canStack: true);
            targetResults.Add(new BattleTargetResult(targetId, 0, 0, 0, false, null));
        }

        return new BattleActionResult(action.ActorId, action.Kind, action.MoveId, targetResults.Count > 0, targetResults: targetResults);
    }

    private BattleTargetResult? ResolveEffect(
        BattleActorSnapshot actorSnapshot,
        Status attackerStatus,
        BattleActorSnapshot targetSnapshot,
        Status defenderStatus,
        BattleActorState targetState,
        Move move,
        MoveEffect effect,
        bool isSupportMove)
    {
        return effect.EffectType switch
        {
            MoveEffectType.Damage => ResolveDamageEffect(actorSnapshot, attackerStatus, targetSnapshot, defenderStatus, targetState, move, effect, isSupportMove),
            MoveEffectType.Heal => ResolveHealEffect(attackerStatus, targetSnapshot, defenderStatus, targetState, move, effect, isSupportMove),
            MoveEffectType.RestoreMp => ResolveRestoreMpEffect(attackerStatus, targetSnapshot, defenderStatus, targetState, move, effect, isSupportMove),
            MoveEffectType.Ailment => ResolveAilmentEffect(actorSnapshot, attackerStatus, defenderStatus, targetState, move, effect),
            MoveEffectType.Buff => ResolveBuffEffect(attackerStatus, defenderStatus, targetState, move, effect),
            MoveEffectType.Knockout => ResolveKnockoutEffect(attackerStatus, defenderStatus, targetState, move, effect),
            _ => throw new ArgumentOutOfRangeException(nameof(effect.EffectType), $"未対応の MoveEffectType: {effect.EffectType}")
        };
    }

    private BattleTargetResult ResolveDamageEffect(
        BattleActorSnapshot actorSnapshot,
        Status attackerStatus,
        BattleActorSnapshot targetSnapshot,
        Status defenderStatus,
        BattleActorState targetState,
        Move move,
        MoveEffect effect,
        bool isSupportMove)
    {
        ArgumentNullException.ThrowIfNull(effect.Damage);
        var attackStat = ResolveAttackStat(move, effect.Damage, attackerStatus, isSupportMove);
        if (!ShouldHit(attackerStatus, effect.OverrideTargetType ?? move.TargetType))
        {
            return new BattleTargetResult(targetSnapshot.Id, 0, 0, 0, targetState.IsDead, null);
        }

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
                attackStat));
            totalDamage += damageResult.Damage;
        }

        targetState.ReceiveDamage(totalDamage);
        return new BattleTargetResult(targetSnapshot.Id, totalDamage, -totalDamage, 0, targetState.IsDead, null);
    }

    private static BattleTargetResult ResolveHealEffect(
        Status attackerStatus,
        BattleActorSnapshot targetSnapshot,
        Status defenderStatus,
        BattleActorState targetState,
        Move move,
        MoveEffect effect,
        bool isSupportMove)
    {
        ArgumentNullException.ThrowIfNull(effect.Damage);

        var attackStat = ResolveAttackStat(move, effect.Damage, attackerStatus, isSupportMove);
        var attackPower = attackStat switch
        {
            BuffStat.Intelligence => attackerStatus.Intelligence,
            BuffStat.Defense => attackerStatus.Defense,
            _ => attackerStatus.Strength
        };
        var healValue = Math.Max(1, effect.Damage.FixedValue + (int)Math.Round(attackPower * effect.Damage.PowerRate, MidpointRounding.AwayFromZero));
        var beforeHp = targetState.CurrentHp;
        targetState.RestoreHp(healValue, defenderStatus.MaxHp);
        var restoredHp = targetState.CurrentHp - beforeHp;

        return new BattleTargetResult(targetSnapshot.Id, 0, restoredHp, 0, false, null);
    }

    private static BattleTargetResult ResolveRestoreMpEffect(
        Status attackerStatus,
        BattleActorSnapshot targetSnapshot,
        Status defenderStatus,
        BattleActorState targetState,
        Move move,
        MoveEffect effect,
        bool isSupportMove)
    {
        ArgumentNullException.ThrowIfNull(effect.Damage);

        var attackStat = ResolveAttackStat(move, effect.Damage, attackerStatus, isSupportMove);
        var attackPower = attackStat switch
        {
            BuffStat.Intelligence => attackerStatus.Intelligence,
            BuffStat.Defense => attackerStatus.Defense,
            _ => attackerStatus.Strength
        };
        var restoreValue = Math.Max(1, effect.Damage.FixedValue + (int)Math.Round(attackPower * effect.Damage.PowerRate, MidpointRounding.AwayFromZero));
        var beforeMp = targetState.CurrentMp;
        targetState.RestoreMp(restoreValue, defenderStatus.MaxMp);
        var restoredMp = targetState.CurrentMp - beforeMp;

        return new BattleTargetResult(targetSnapshot.Id, 0, 0, restoredMp, false, null);
    }

    private BattleTargetResult ResolveAilmentEffect(
        BattleActorSnapshot actorSnapshot,
        Status attackerStatus,
        Status defenderStatus,
        BattleActorState targetState,
        Move move,
        MoveEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect.Ailment);

        var appliedAilment = default(AilmentType?);
        if (ShouldHit(attackerStatus, effect.OverrideTargetType ?? move.TargetType) &&
            ShouldApplySecondaryEffect(effect.Ailment.AilmentRate, attackerStatus, defenderStatus))
        {
            if (effect.Ailment.AilmentType == AilmentType.InstantDeath)
            {
                targetState.ReceiveDamage(targetState.CurrentHp);
            }
            else
            {
                targetState.ApplyAilment(new BattleAilmentState(
                    effect.Ailment.AilmentType,
                    effect.Ailment.AilmentTurns,
                    effect.Ailment.TriggerDamage,
                    actorSnapshot.MoveSet.GetLearnedMoveIds().FirstOrDefault(id => id.Id == effect.MoveId.Id) ?? effect.MoveId));
            }

            appliedAilment = effect.Ailment.AilmentType;
        }

        return new BattleTargetResult(targetState.Id, 0, 0, 0, targetState.IsDead, appliedAilment);
    }

    private BattleTargetResult ResolveBuffEffect(
        Status attackerStatus,
        Status defenderStatus,
        BattleActorState targetState,
        Move move,
        MoveEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect.Buff);

        if (ShouldHit(attackerStatus, effect.OverrideTargetType ?? move.TargetType) &&
            ShouldApplySecondaryEffect(effect.Buff.BuffRate, attackerStatus, defenderStatus))
        {
            targetState.ApplyBuff(
                new BattleBuffState(effect.Buff.BuffStat, effect.Buff.BuffCalculationType, effect.Buff.BuffValue, effect.Buff.BuffTurns),
                effect.Buff.CanStack);
        }

        return new BattleTargetResult(targetState.Id, 0, 0, 0, targetState.IsDead, null);
    }

    private BattleTargetResult ResolveKnockoutEffect(
        Status attackerStatus,
        Status defenderStatus,
        BattleActorState targetState,
        Move move,
        MoveEffect effect)
    {
        if (ShouldHit(attackerStatus, effect.OverrideTargetType ?? move.TargetType) &&
            ShouldApplySecondaryEffect(1m, attackerStatus, defenderStatus))
        {
            targetState.ReceiveDamage(targetState.CurrentHp);
        }

        return new BattleTargetResult(targetState.Id, 0, 0, 0, targetState.IsDead, null);
    }

    private static BuffStat ResolveAttackStat(Move move, DamageEffect effect, Status attackerStatus, bool isSupportMove)
    {
        if (effect.AttackStat is not null)
        {
            return effect.AttackStat.Value;
        }

        if (isSupportMove || move.Category != MoveCategory.Attack)
        {
            return BuffStat.Intelligence;
        }

        return attackerStatus.Intelligence > attackerStatus.Strength
            ? BuffStat.Intelligence
            : BuffStat.Strength;
    }

    private static bool ShouldApplySecondaryEffect(decimal rate, Status attackerStatus, Status defenderStatus)
    {
        return rate >= 1m || (rate > 0m && attackerStatus.Luck >= defenderStatus.Luck);
    }

    private bool ShouldSkipActionByParalysis(BattleActorState actorState)
    {
        if (!actorState.Ailments.Any(x => x.Type == AilmentType.Paralysis))
        {
            return false;
        }

        return _randomProvider() < 0.3d;
    }

    private IReadOnlyList<BattleActorId> ResolveTargets(
        BattleTargetSelector selector,
        BattleActorSnapshot actorSnapshot,
        IEnumerable<BattleActorSnapshot> snapshots,
        IEnumerable<BattleActorState> states,
        BattleFieldContext? fieldContext)
    {
        return battleTargetingResolver.ResolveTargets(selector, actorSnapshot, snapshots, states, fieldContext);
    }

    private IReadOnlyList<BattleActorId> ResolveEffectTargets(
        BattleAction action,
        Move move,
        MoveEffect effect,
        BattleActorSnapshot actorSnapshot,
        IEnumerable<BattleActorSnapshot> snapshots,
        IEnumerable<BattleActorState> states,
        BattleFieldContext? fieldContext,
        IReadOnlyList<BattleActorId> initialTargets)
    {
        if (effect.OverrideTargetType is null &&
            effect.OverrideAttackRange is null &&
            effect.OverrideTargetLifeState is null)
        {
            return initialTargets;
        }

        return ResolveTargets(
            new BattleTargetSelector(
                effect.OverrideTargetType ?? move.TargetType,
                effect.OverrideAttackRange ?? move.AttackRange,
                selectedPosition: action.Target.SelectedPosition,
                targetLifeState: effect.OverrideTargetLifeState ?? move.TargetLifeState),
            actorSnapshot,
            snapshots,
            states,
            fieldContext);
    }

    private bool ShouldHit(Status attackerStatus, TargetType targetType)
    {
        if (targetType is TargetType.Ally or TargetType.Self)
        {
            return true;
        }

        return attackerStatus.Accuracy >= 100 || _randomProvider() * 100d < attackerStatus.Accuracy;
    }
}
