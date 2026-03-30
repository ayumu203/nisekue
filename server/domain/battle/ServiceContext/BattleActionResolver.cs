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
            var receiverId = battleTargetingResolver.ResolveDamageReceiver(targetId, snapshotMap.Values, stateMap.Values);
            var targetSnapshot = snapshotMap[receiverId];
            var targetState = stateMap[receiverId];
            if (targetState.IsDead)
            {
                continue;
            }

            var defenderStatus = battleStatusResolver.BuildEffectiveStatus(targetSnapshot, targetState);
            if (!ShouldHit(attackerStatus, defenderStatus, TargetType.Enemy))
            {
                targetResults.Add(new BattleTargetResult(receiverId, 0, 0, 0, targetState.IsDead, null));
                continue;
            }

            var damageResult = battleDamageCalculator.Calculate(new BattleDamageInput(
                actorSnapshot.Id,
                targetId,
                attackerStatus,
                defenderStatus,
                fixedPower: 0,
                powerRate: 1m,
                criticalRate: 0m,
                criticalChanceBonus: 0m,
                elementType: ElementType.None,
                attackStat: BuffStat.Strength));

            targetState.ReceiveDamage(damageResult.Damage);
            targetResults.Add(new BattleTargetResult(receiverId, damageResult.Damage, -damageResult.Damage, 0, targetState.IsDead, null));
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
                    actorState,
                    attackerStatus,
                    targetSnapshot,
                    defenderStatus,
                    targetState,
                    snapshotMap.Values,
                    stateMap.Values,
                    move,
                    effect,
                    move.Category != MoveCategory.Attack,
                    fieldContext);
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
        BattleActorState actorState,
        Status attackerStatus,
        BattleActorSnapshot targetSnapshot,
        Status defenderStatus,
        BattleActorState targetState,
        IEnumerable<BattleActorSnapshot> snapshots,
        IEnumerable<BattleActorState> states,
        Move move,
        MoveEffect effect,
        bool isSupportMove,
        BattleFieldContext? fieldContext)
    {
        return effect.EffectType switch
        {
            MoveEffectType.Damage => ResolveDamageEffect(actorSnapshot, actorState, attackerStatus, targetSnapshot, defenderStatus, targetState, snapshots, states, move, effect, isSupportMove),
            MoveEffectType.Heal => ResolveHealEffect(attackerStatus, targetSnapshot, defenderStatus, targetState, move, effect, isSupportMove),
            MoveEffectType.RestoreMp => ResolveRestoreMpEffect(attackerStatus, targetSnapshot, defenderStatus, targetState, move, effect, isSupportMove),
            MoveEffectType.Ailment => ResolveAilmentEffect(actorSnapshot, attackerStatus, targetSnapshot, defenderStatus, targetState, move, effect, fieldContext),
            MoveEffectType.Buff => ResolveBuffEffect(attackerStatus, defenderStatus, targetState, move, effect),
            MoveEffectType.Knockout => ResolveKnockoutEffect(attackerStatus, defenderStatus, targetState, move, effect),
            MoveEffectType.HalveSelfHp => ResolveHalveSelfHpEffect(targetState),
            _ => throw new ArgumentOutOfRangeException(nameof(effect.EffectType), $"未対応の MoveEffectType: {effect.EffectType}")
        };
    }

    private BattleTargetResult ResolveDamageEffect(
        BattleActorSnapshot actorSnapshot,
        BattleActorState actorState,
        Status attackerStatus,
        BattleActorSnapshot targetSnapshot,
        Status defenderStatus,
        BattleActorState targetState,
        IEnumerable<BattleActorSnapshot> snapshots,
        IEnumerable<BattleActorState> states,
        Move move,
        MoveEffect effect,
        bool isSupportMove)
    {
        ArgumentNullException.ThrowIfNull(effect.Damage);
        var attackStat = ResolveAttackStat(move, effect.Damage, attackerStatus, isSupportMove);
        var receiverSnapshot = targetSnapshot;
        var receiverState = targetState;
        var receiverStatus = defenderStatus;

        if ((effect.OverrideTargetType ?? move.TargetType) == TargetType.Enemy)
        {
            var receiverId = battleTargetingResolver.ResolveDamageReceiver(targetSnapshot.Id, snapshots, states);
            if (receiverId != targetSnapshot.Id)
            {
                var snapshotMap = snapshots.ToDictionary(x => x.Id);
                var stateMap = states.ToDictionary(x => x.Id);
                receiverSnapshot = snapshotMap[receiverId];
                receiverState = stateMap[receiverId];
                receiverStatus = battleStatusResolver.BuildEffectiveStatus(receiverSnapshot, receiverState);
            }
        }

        if (!ShouldHit(attackerStatus, receiverStatus, effect.OverrideTargetType ?? move.TargetType))
        {
            return new BattleTargetResult(receiverSnapshot.Id, 0, 0, 0, receiverState.IsDead, null);
        }

        var totalDamage = 0;
        var criticalChanceBonus = actorState.GetBuffTotal(BuffStat.CriticalChance, BuffCalculationType.Add) / 100m;
        for (var i = 0; i < effect.Damage.HitCount; i++)
        {
            var damageResult = battleDamageCalculator.Calculate(new BattleDamageInput(
                actorSnapshot.Id,
                receiverSnapshot.Id,
                attackerStatus,
                receiverStatus,
                effect.Damage.FixedValue,
                effect.Damage.PowerRate,
                effect.Damage.CriticalRate,
                criticalChanceBonus,
                effect.Damage.ElementType,
                attackStat));
            totalDamage += damageResult.Damage;
        }

        if (criticalChanceBonus > 0m)
        {
            actorState.RemoveBuffs(BuffStat.CriticalChance);
        }

        receiverState.ReceiveDamage(totalDamage);
        return new BattleTargetResult(receiverSnapshot.Id, totalDamage, -totalDamage, 0, receiverState.IsDead, null);
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
        BattleActorSnapshot targetSnapshot,
        Status defenderStatus,
        BattleActorState targetState,
        Move move,
        MoveEffect effect,
        BattleFieldContext? fieldContext)
    {
        ArgumentNullException.ThrowIfNull(effect.Ailment);

        var appliedAilment = default(AilmentType?);
        if (ShouldHit(attackerStatus, defenderStatus, effect.OverrideTargetType ?? move.TargetType) &&
            ShouldApplySecondaryEffect(effect.Ailment.AilmentRate, attackerStatus, defenderStatus))
        {
            if (effect.Ailment.AilmentType == AilmentType.InstantDeath)
            {
                var isBossTarget = fieldContext?.IsBossActor(targetSnapshot.Id) ?? false;
                if (!isBossTarget || effect.Ailment.AllowBossInstantDeath)
                {
                    targetState.ReceiveDamage(targetState.CurrentHp);
                    appliedAilment = effect.Ailment.AilmentType;
                }
            }
            else
            {
                targetState.ApplyAilment(new BattleAilmentState(
                    effect.Ailment.AilmentType,
                    effect.Ailment.AilmentTurns,
                    effect.Ailment.TriggerDamage,
                    actorSnapshot.MoveSet.GetLearnedMoveIds().FirstOrDefault(id => id.Id == effect.MoveId.Id) ?? effect.MoveId,
                    effect.Ailment.AilmentType == AilmentType.Regeneration ? defenderStatus.MaxHp : null));
                appliedAilment = effect.Ailment.AilmentType;
            }
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

        if (ShouldHit(attackerStatus, defenderStatus, effect.OverrideTargetType ?? move.TargetType) &&
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
        if (ShouldHit(attackerStatus, defenderStatus, effect.OverrideTargetType ?? move.TargetType) &&
            ShouldApplySecondaryEffect(1m, attackerStatus, defenderStatus))
        {
            targetState.ReceiveDamage(targetState.CurrentHp);
        }

        return new BattleTargetResult(targetState.Id, 0, 0, 0, targetState.IsDead, null);
    }

    private static BattleTargetResult ResolveHalveSelfHpEffect(BattleActorState targetState)
    {
        var nextHp = Math.Max(1, (int)Math.Ceiling(targetState.CurrentHp / 2m));
        var consumedHp = Math.Max(0, targetState.CurrentHp - nextHp);
        targetState.ReceiveDamage(consumedHp);
        return new BattleTargetResult(targetState.Id, consumedHp, -consumedHp, 0, false, null);
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

    private bool ShouldHit(Status attackerStatus, Status defenderStatus, TargetType targetType)
    {
        if (targetType is TargetType.Ally or TargetType.Self)
        {
            return true;
        }

        var hitChance = Math.Clamp(attackerStatus.Accuracy - defenderStatus.Evasion, 0, 100);
        return hitChance >= 100 || _randomProvider() * 100d < hitChance;
    }
}
