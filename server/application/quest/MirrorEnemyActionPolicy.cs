using server.application.battle;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.quest;

namespace server.application.quest;

/// <summary>名鑑の祠の敵が選んだ行動。蘇生を選んだかどうかを呼び出し側で数えるために持つ。</summary>
public sealed record MirrorEnemyActionDecision(BattleActionInput Action, bool IsRevive);

/// <summary>
/// 名鑑の祠の敵（プレイヤー）の行動を職業から決める。
/// docs/quest/06_名鑑の祠設計.md の「3. 敵の行動」を実装する。
/// 既存の敵 AI（QuestEnemyActionPolicy）は他のステージで使うので変更しない。
/// </summary>
public class MirrorEnemyActionPolicy
{
    /// <summary>第2候補を選ぶ確率。</summary>
    public const double SecondChoiceRate = 0.15;

    /// <summary>蘇生技を使う敵1体が1フロアで蘇生できる回数。</summary>
    public const int MaxRevivesPerFloor = 3;

    private const int SelfHarmMinHpPercent = 20;
    private const int HealHpPercent = 50;
    private const int RestoreMpPercent = 30;

    // 自分へのバフは、使える状況でも半分の確率で見送り、攻撃などほかの行動に進む。
    // 毎回の初手がバフだと単調になるため。
    private const double SelfBuffRate = 0.5;

    // 自分にかけるバフは職業の攻撃に効く能力だけ（設計 3.4）。
    // 能力を限定しないと、バフ技を2種類以上持つ敵がかけ直しを続けて攻撃しなくなる。
    private static readonly IReadOnlySet<BuffStat> WarriorSelfBuffStats =
        new HashSet<BuffStat> { BuffStat.Strength, BuffStat.StrengthIntelligence };

    private static readonly IReadOnlySet<BuffStat> MageSelfBuffStats =
        new HashSet<BuffStat> { BuffStat.Intelligence, BuffStat.StrengthIntelligence };

    private readonly BattleTargetingResolver targetingResolver = new();
    private readonly Func<double> nextDouble;
    private readonly Func<double> nextSelfBuffRoll;

    /// <param name="nextDouble">第2候補を選ぶかの乱数。</param>
    /// <param name="nextSelfBuffRoll">自分へのバフを使うかの乱数。<see cref="SelfBuffRate"/> 未満なら使う。</param>
    public MirrorEnemyActionPolicy(Func<double>? nextDouble = null, Func<double>? nextSelfBuffRoll = null)
    {
        this.nextDouble = nextDouble ?? Random.Shared.NextDouble;
        this.nextSelfBuffRoll = nextSelfBuffRoll ?? Random.Shared.NextDouble;
    }

    public MirrorEnemyActionDecision SelectAction(
        QuestRun run,
        QuestEnemyState enemy,
        MirrorEnemySnapshot snapshot,
        IReadOnlyList<Move> availableMoves,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(availableMoves);
        ArgumentNullException.ThrowIfNull(enemyDefinitions);

        var context = CreateContext(run, enemy, snapshot, enemyDefinitions);
        var usableMoves = availableMoves
            .Where(move => enemy.CurrentMp >= move.MpCost)
            .Where(move => !KnocksOutSelf(move))
            .Where(move => !HalvesSelfHp(move) || IsAtLeastPercent(enemy.CurrentHp, snapshot.Status.MaxHp, SelfHarmMinHpPercent))
            .ToArray();

        var candidates = BuildCandidates(MirrorJobRowPolicy.ResolveGroup(snapshot.Job), enemy, availableMoves, usableMoves, context);
        if (candidates.Count == 0)
        {
            return new MirrorEnemyActionDecision(CreateWait(enemy), IsRevive: false);
        }

        if (candidates.Count > 1 && nextDouble() < SecondChoiceRate)
        {
            return candidates[1];
        }

        return candidates[0];
    }

    private List<MirrorEnemyActionDecision> BuildCandidates(
        MirrorJobGroup group,
        QuestEnemyState enemy,
        IReadOnlyList<Move> availableMoves,
        IReadOnlyList<Move> usableMoves,
        Context context)
    {
        var steps = new List<Func<MirrorEnemyActionDecision?>>();
        switch (group)
        {
            case MirrorJobGroup.Warrior:
                steps.Add(() => TrySelfBuff(enemy, usableMoves, context, WarriorSelfBuffStats));
                steps.Add(() => TryBestAttack(enemy, usableMoves, context));
                break;
            case MirrorJobGroup.Guardian:
                steps.Add(() => TryCover(enemy, usableMoves, context));
                if (!availableMoves.Any(move => HasAilment(move, AilmentType.CoverAll)))
                {
                    steps.Add(() => TryTaunt(enemy, usableMoves, context));
                }

                steps.Add(() => TrySupportBuff(enemy, usableMoves, context, BuffStat.Defense));
                steps.Add(() => TryBestAttack(enemy, usableMoves, context));
                break;
            case MirrorJobGroup.Mage:
                steps.Add(() => TrySelfBuff(enemy, usableMoves, context, MageSelfBuffStats));
                steps.Add(() => TryBestAttack(enemy, usableMoves, context));
                steps.Add(() => TryRestoreMp(enemy, usableMoves, context));
                break;
            case MirrorJobGroup.Priest:
                steps.Add(() => TryRevive(enemy, usableMoves, context));
                steps.Add(() => TryHeal(enemy, usableMoves, context));
                steps.Add(() => TryRegeneration(enemy, usableMoves, context));
                steps.Add(() => TrySupportBuff(enemy, usableMoves, context, BuffStat.Defense));
                steps.Add(() => TryBestAttack(enemy, usableMoves, context));
                break;
            case MirrorJobGroup.Ranger:
                steps.Add(() => TryTrap(enemy, usableMoves, context));
                steps.Add(() => TryStatusAilment(enemy, usableMoves, context));
                steps.Add(() => TryBestAttack(enemy, usableMoves, context));
                break;
            default:
                steps.Add(() => TryBestAttack(enemy, usableMoves, context));
                break;
        }

        steps.Add(() => TryNormalAttack(enemy, context));

        var candidates = new List<MirrorEnemyActionDecision>();
        foreach (var step in steps)
        {
            var decision = step();
            if (decision is not null && !candidates.Any(x => x.Action == decision.Action))
            {
                candidates.Add(decision);
            }
        }

        return candidates;
    }

    // --- 行動ごとの候補 ---

    private MirrorEnemyActionDecision? TryRevive(QuestEnemyState enemy, IReadOnlyList<Move> usableMoves, Context context)
    {
        if (enemy.ReviveUsedCount >= MaxRevivesPerFloor)
        {
            return null;
        }

        var reviveMoves = usableMoves
            .Where(IsReviveMove)
            .OrderBy(move => move.MpCost)
            .ToArray();
        foreach (var target in context.Friends.Where(x => x.CurrentHp <= 0).OrderByDescending(x => x.MaxHp))
        {
            var move = reviveMoves.FirstOrDefault(candidate => CanTarget(candidate, target, context));
            if (move is not null)
            {
                return new MirrorEnemyActionDecision(CreateMoveAction(enemy, move, target), IsRevive: true);
            }
        }

        return null;
    }

    private MirrorEnemyActionDecision? TryHeal(QuestEnemyState enemy, IReadOnlyList<Move> usableMoves, Context context)
    {
        var healMoves = usableMoves
            .Where(move => move.TargetType != TargetType.Enemy)
            .Where(move => move.TargetLifeState != TargetLifeState.Dead)
            .Where(move => move.Effects.Any(effect => effect.EffectType == MoveEffectType.Heal))
            .ToArray();
        if (healMoves.Length == 0)
        {
            return null;
        }

        var targets = context.Friends
            .Append(context.Self)
            .Where(x => x.CurrentHp > 0 && !IsAbovePercent(x.CurrentHp, x.MaxHp, HealHpPercent))
            .OrderBy(x => HpRatio(x));
        foreach (var target in targets)
        {
            var move = healMoves
                .Where(candidate => CanTarget(candidate, target, context))
                .OrderBy(candidate => Math.Abs(target.CurrentHp + EstimateHealAmount(context.Status, candidate) - target.MaxHp))
                .ThenBy(candidate => candidate.MpCost)
                .FirstOrDefault();
            if (move is not null)
            {
                return new MirrorEnemyActionDecision(CreateMoveAction(enemy, move, target), IsRevive: false);
            }
        }

        return null;
    }

    private MirrorEnemyActionDecision? TrySelfBuff(
        QuestEnemyState enemy,
        IReadOnlyList<Move> usableMoves,
        Context context,
        IReadOnlySet<BuffStat> stats)
    {
        // 対象の能力のバフが1つでもかかっていれば、かけ直さずに攻撃へ進む。
        if (stats.Any(stat => HasBuff(context.Self, stat)))
        {
            return null;
        }

        var buffDecision = SelectSelfBuff(enemy, usableMoves, context, stats);
        if (buffDecision is null)
        {
            return null;
        }

        // 使えるバフがあっても半分の確率で見送る。
        return nextSelfBuffRoll() < SelfBuffRate ? buffDecision : null;
    }

    private MirrorEnemyActionDecision? SelectSelfBuff(
        QuestEnemyState enemy,
        IReadOnlyList<Move> usableMoves,
        Context context,
        IReadOnlySet<BuffStat> stats)
    {
        var buffMoves = usableMoves
            .Where(move => move.TargetType != TargetType.Enemy)
            .Where(move => GetPositiveBuffStats(move).Any(stats.Contains))
            .Where(move => !HasAilment(move, AilmentType.Taunt) && !HasAilment(move, AilmentType.CoverAll))
            .Where(move => CanTarget(move, context.Self, context))
            .OrderByDescending(move => CountMissingBuffStats(move, context.Self))
            .ThenBy(move => move.MpCost)
            .ToArray();

        return buffMoves.Length == 0
            ? null
            : new MirrorEnemyActionDecision(CreateMoveAction(enemy, buffMoves[0], context.Self), IsRevive: false);
    }

    private MirrorEnemyActionDecision? TrySupportBuff(QuestEnemyState enemy, IReadOnlyList<Move> usableMoves, Context context, BuffStat stat)
    {
        var buffMoves = usableMoves
            .Where(move => move.TargetType != TargetType.Enemy)
            .Where(move => HasPositiveBuff(move, stat))
            .Where(move => !HasAilment(move, AilmentType.Taunt) && !HasAilment(move, AilmentType.CoverAll))
            .OrderBy(move => move.MpCost)
            .ToArray();
        if (buffMoves.Length == 0)
        {
            return null;
        }

        var targets = context.Friends
            .Append(context.Self)
            .Where(x => x.CurrentHp > 0 && !HasBuff(x, stat))
            .OrderBy(x => HpRatio(x));
        foreach (var target in targets)
        {
            var move = buffMoves.FirstOrDefault(candidate => CanTarget(candidate, target, context));
            if (move is not null)
            {
                return new MirrorEnemyActionDecision(CreateMoveAction(enemy, move, target), IsRevive: false);
            }
        }

        return null;
    }

    private MirrorEnemyActionDecision? TryRegeneration(QuestEnemyState enemy, IReadOnlyList<Move> usableMoves, Context context)
    {
        var regenerationMoves = usableMoves
            .Where(move => move.TargetType != TargetType.Enemy)
            .Where(move => HasAilment(move, AilmentType.Regeneration))
            .OrderBy(move => move.MpCost)
            .ToArray();
        if (regenerationMoves.Length == 0)
        {
            return null;
        }

        var targets = context.Friends
            .Append(context.Self)
            .Where(x => x.CurrentHp > 0 && !HasAilmentState(x, AilmentType.Regeneration))
            .OrderBy(x => HpRatio(x));
        foreach (var target in targets)
        {
            var move = regenerationMoves.FirstOrDefault(candidate => CanTarget(candidate, target, context));
            if (move is not null)
            {
                return new MirrorEnemyActionDecision(CreateMoveAction(enemy, move, target), IsRevive: false);
            }
        }

        return null;
    }

    private MirrorEnemyActionDecision? TryCover(QuestEnemyState enemy, IReadOnlyList<Move> usableMoves, Context context)
    {
        if (HasAilmentState(context.Self, AilmentType.CoverAll))
        {
            return null;
        }

        var move = usableMoves
            .Where(candidate => HasAilment(candidate, AilmentType.CoverAll))
            .Where(candidate => CanTarget(candidate, context.Self, context))
            .OrderBy(candidate => candidate.MpCost)
            .FirstOrDefault();

        return move is null
            ? null
            : new MirrorEnemyActionDecision(CreateMoveAction(enemy, move, context.Self), IsRevive: false);
    }

    private MirrorEnemyActionDecision? TryTaunt(QuestEnemyState enemy, IReadOnlyList<Move> usableMoves, Context context)
    {
        if (HasAilmentState(context.Self, AilmentType.Taunt))
        {
            return null;
        }

        var move = usableMoves
            .Where(candidate => HasAilment(candidate, AilmentType.Taunt))
            .Where(candidate => CanTarget(candidate, context.Self, context))
            .OrderBy(candidate => candidate.MpCost)
            .FirstOrDefault();

        return move is null
            ? null
            : new MirrorEnemyActionDecision(CreateMoveAction(enemy, move, context.Self), IsRevive: false);
    }

    private MirrorEnemyActionDecision? TryRestoreMp(QuestEnemyState enemy, IReadOnlyList<Move> usableMoves, Context context)
    {
        if (IsAbovePercent(enemy.CurrentMp, context.Status.MaxMp, RestoreMpPercent))
        {
            return null;
        }

        var move = usableMoves
            .Where(candidate => candidate.TargetType != TargetType.Enemy)
            .Where(candidate => candidate.Effects.Any(effect => effect.EffectType == MoveEffectType.RestoreMp))
            .Where(candidate => CanTarget(candidate, context.Self, context))
            .OrderBy(candidate => candidate.MpCost)
            .FirstOrDefault();

        return move is null
            ? null
            : new MirrorEnemyActionDecision(CreateMoveAction(enemy, move, context.Self), IsRevive: false);
    }

    private MirrorEnemyActionDecision? TryTrap(QuestEnemyState enemy, IReadOnlyList<Move> usableMoves, Context context)
    {
        // 相手側にすでにトラップがあれば重ねない。
        if (context.Opponents.Any(x => HasAilmentState(x, AilmentType.DamageTrap) || HasAilmentState(x, AilmentType.PoisonTrap)))
        {
            return null;
        }

        var trapMoves = usableMoves
            .Where(move => move.TargetType == TargetType.Enemy)
            .Where(move => HasAilment(move, AilmentType.DamageTrap) || HasAilment(move, AilmentType.PoisonTrap))
            .OrderBy(move => move.MpCost)
            .ToArray();

        return TryTargetLowestHpOpponent(enemy, trapMoves, context, _ => true);
    }

    private MirrorEnemyActionDecision? TryStatusAilment(QuestEnemyState enemy, IReadOnlyList<Move> usableMoves, Context context)
    {
        var statusMoves = usableMoves
            .Where(move => move.TargetType == TargetType.Enemy)
            .Where(move => move.Effects.All(effect => effect.EffectType != MoveEffectType.Damage))
            .Where(move => GetStatusAilmentTypes(move).Count > 0)
            .OrderBy(move => move.MpCost)
            .ToArray();

        foreach (var move in statusMoves)
        {
            var ailmentTypes = GetStatusAilmentTypes(move);
            var decision = TryTargetLowestHpOpponent(
                enemy,
                [move],
                context,
                target => ailmentTypes.Any(type => !HasAilmentState(target, type)));
            if (decision is not null)
            {
                return decision;
            }
        }

        return null;
    }

    private MirrorEnemyActionDecision? TryBestAttack(QuestEnemyState enemy, IReadOnlyList<Move> usableMoves, Context context)
    {
        var damageMoves = usableMoves
            .Where(move => move.TargetType == TargetType.Enemy)
            .Where(move => move.Effects.Any(effect => effect.EffectType == MoveEffectType.Damage))
            .ToArray();

        (Move Move, Target Target, decimal Value, int LowestHp)? best = null;
        foreach (var move in damageMoves)
        {
            foreach (var target in context.Opponents)
            {
                var resolved = ResolveTargets(move.TargetType, move.AttackRange, move.TargetLifeState, target.Position, context);
                var hitOpponents = resolved.Where(x => context.OpponentIds.Contains(x.ActorId)).ToArray();
                if (hitOpponents.Length == 0)
                {
                    continue;
                }

                var value = EstimateDamage(context.Status, move) * hitOpponents.Length;
                var lowestHp = hitOpponents.Min(x => x.CurrentHp);
                if (best is null || IsBetterAttack(move, target, value, lowestHp, best.Value, context))
                {
                    best = (move, target, value, lowestHp);
                }
            }
        }

        return best is null
            ? null
            : new MirrorEnemyActionDecision(CreateMoveAction(enemy, best.Value.Move, best.Value.Target), IsRevive: false);
    }

    private MirrorEnemyActionDecision? TryNormalAttack(QuestEnemyState enemy, Context context)
    {
        foreach (var target in OrderOpponents(context))
        {
            var resolved = ResolveTargets(TargetType.Enemy, AttackRange.Single, TargetLifeState.Alive, target.Position, context);
            if (resolved.Count == 0)
            {
                continue;
            }

            return new MirrorEnemyActionDecision(
                new BattleActionInput(
                    ActorId: enemy.Id.Value,
                    Kind: BattleActionKind.NormalAttack,
                    MoveId: null,
                    TargetType: TargetType.Enemy,
                    AttackRange: AttackRange.Single,
                    SelectedPosition: target.Position),
                IsRevive: false);
        }

        return null;
    }

    private MirrorEnemyActionDecision? TryTargetLowestHpOpponent(
        QuestEnemyState enemy,
        IReadOnlyList<Move> moves,
        Context context,
        Func<Target, bool> needs)
    {
        foreach (var move in moves)
        {
            foreach (var target in OrderOpponents(context).Where(needs))
            {
                if (CanTarget(move, target, context))
                {
                    return new MirrorEnemyActionDecision(CreateMoveAction(enemy, move, target), IsRevive: false);
                }
            }
        }

        return null;
    }

    // --- 判定 ---

    /// <summary>見込みダメージが大きい方、同じなら MP が安い方、さらに同じなら HP が低い相手・職業の好む列を狙う方。</summary>
    private static bool IsBetterAttack(
        Move move,
        Target target,
        decimal value,
        int lowestHp,
        (Move Move, Target Target, decimal Value, int LowestHp) best,
        Context context)
    {
        if (value != best.Value)
        {
            return value > best.Value;
        }

        if (move.MpCost != best.Move.MpCost)
        {
            return move.MpCost < best.Move.MpCost;
        }

        if (lowestHp != best.LowestHp)
        {
            return lowestHp < best.LowestHp;
        }

        return GetRowPreference(target.Position, context.PrefersBackRow) < GetRowPreference(best.Target.Position, context.PrefersBackRow);
    }

    /// <summary>単体で狙う順：現在 HP が低い順、同じなら職業の好む列から。</summary>
    private static IEnumerable<Target> OrderOpponents(Context context)
    {
        return context.Opponents
            .OrderBy(x => x.CurrentHp)
            .ThenBy(x => GetRowPreference(x.Position, context.PrefersBackRow));
    }

    private static int GetRowPreference(BattlePosition position, bool prefersBackRow)
    {
        var rowOrder = position.Row switch
        {
            BattleRow.Front => 0,
            BattleRow.Middle => 1,
            _ => 2
        };
        if (prefersBackRow)
        {
            rowOrder = 2 - rowOrder;
        }

        return (rowOrder * 2) + (position.Column == BattleColumn.Left ? 0 : 1);
    }

    private bool CanTarget(Move move, Target target, Context context)
    {
        if (move.TargetType == TargetType.Self)
        {
            return target.ActorId == context.Self.ActorId;
        }

        var resolved = ResolveTargets(move.TargetType, move.AttackRange, move.TargetLifeState, target.Position, context);
        return resolved.Any(x => x.ActorId == target.ActorId);
    }

    private IReadOnlyList<Target> ResolveTargets(
        TargetType targetType,
        AttackRange attackRange,
        TargetLifeState targetLifeState,
        BattlePosition selectedPosition,
        Context context)
    {
        var selector = new BattleTargetSelector(targetType, attackRange, selectedPosition: selectedPosition, targetLifeState: targetLifeState);
        var targetIds = targetingResolver.ResolveTargets(selector, context.Self.Snapshot, context.Snapshots, context.States, context.FieldContext);
        return targetIds
            .Where(context.TargetsById.ContainsKey)
            .Select(id => context.TargetsById[id])
            .ToArray();
    }

    private static bool IsReviveMove(Move move)
    {
        return move.TargetType != TargetType.Enemy &&
               move.TargetLifeState == TargetLifeState.Dead &&
               move.Effects.Any(effect => effect.EffectType == MoveEffectType.Heal);
    }

    private static bool KnocksOutSelf(Move move)
    {
        return move.Effects.Any(effect =>
            effect.EffectType == MoveEffectType.Knockout &&
            (effect.OverrideTargetType ?? move.TargetType) == TargetType.Self);
    }

    private static bool HalvesSelfHp(Move move)
    {
        return move.Effects.Any(effect => effect.EffectType == MoveEffectType.HalveSelfHp);
    }

    private static bool HasAilment(Move move, AilmentType ailmentType)
    {
        return move.Effects.Any(effect =>
            effect.EffectType == MoveEffectType.Ailment &&
            effect.Ailment?.AilmentType == ailmentType);
    }

    private static IReadOnlyList<AilmentType> GetStatusAilmentTypes(Move move)
    {
        return move.Effects
            .Where(effect => effect.EffectType == MoveEffectType.Ailment && effect.Ailment is not null)
            .Select(effect => effect.Ailment!.AilmentType)
            .Where(type => type is not (AilmentType.DamageTrap or AilmentType.PoisonTrap or AilmentType.InstantDeath))
            .Distinct()
            .ToArray();
    }

    private static bool HasPositiveBuff(Move move, BuffStat? stat = null)
    {
        return GetPositiveBuffStats(move).Any(x => stat is null || x == stat);
    }

    private static IReadOnlyList<BuffStat> GetPositiveBuffStats(Move move)
    {
        return move.Effects
            .Where(effect => effect.EffectType == MoveEffectType.Buff && effect.Buff is not null)
            .Where(effect => IsPositiveBuff(effect.Buff!))
            .Select(effect => effect.Buff!.BuffStat)
            .Distinct()
            .ToArray();
    }

    private static bool IsPositiveBuff(BuffEffect buff)
    {
        return IsPositive(buff.BuffCalculationType, buff.BuffValue);
    }

    private static bool IsPositive(BuffCalculationType calculationType, decimal value)
    {
        return calculationType switch
        {
            BuffCalculationType.Mul => value > 1m,
            _ => value > 0m
        };
    }

    private static int CountMissingBuffStats(Move move, Target target)
    {
        return GetPositiveBuffStats(move).Count(stat => !HasBuff(target, stat));
    }

    /// <summary>上げる向きのバフがかかっているか。下げられている（デバフ）だけならかかっていない扱い。</summary>
    private static bool HasBuff(Target target, BuffStat stat)
    {
        return target.State.Buffs.Any(buff => buff.Stat == stat && IsPositive(buff.CalculationType, buff.Value));
    }

    private static bool HasAilmentState(Target target, AilmentType ailmentType)
    {
        return target.State.Ailments.Any(ailment => ailment.Type == ailmentType);
    }

    private static bool IsAtLeastPercent(int current, int max, int percent)
    {
        return max > 0 && current * 100 >= max * percent;
    }

    private static bool IsAbovePercent(int current, int max, int percent)
    {
        return max > 0 && current * 100 > max * percent;
    }

    private static double HpRatio(Target target)
    {
        return target.MaxHp <= 0 ? 0 : (double)target.CurrentHp / target.MaxHp;
    }

    /// <summary>見込みダメージ（1体あたり）＝ Σ(威力倍率×攻撃ステータス＋固定値)×ヒット数。</summary>
    private static decimal EstimateDamage(Status status, Move move)
    {
        return move.Effects
            .Where(effect => effect.EffectType == MoveEffectType.Damage && effect.Damage is not null)
            .Sum(effect =>
            {
                var damage = effect.Damage!;
                var attackPower = ResolveAttackPower(status, move, damage);
                return ((damage.PowerRate * attackPower) + damage.FixedValue) * damage.HitCount;
            });
    }

    private static int EstimateHealAmount(Status status, Move move)
    {
        var damage = move.Effects
            .Where(effect => effect.EffectType == MoveEffectType.Heal)
            .Select(effect => effect.Damage)
            .FirstOrDefault(effect => effect is not null);
        if (damage is null)
        {
            return 0;
        }

        return Math.Max(1, damage.FixedValue + (int)Math.Round(ResolveAttackPower(status, move, damage) * damage.PowerRate, MidpointRounding.AwayFromZero));
    }

    // 攻撃ステータスの決め方は戦闘の解決（BattleActionResolver.ResolveAttackStat）に合わせる。
    private static int ResolveAttackPower(Status status, Move move, DamageEffect damage)
    {
        var attackStat = damage.AttackStat
            ?? (move.Category != MoveCategory.Attack
                ? BuffStat.Intelligence
                : status.Intelligence > status.Strength ? BuffStat.Intelligence : BuffStat.Strength);

        return attackStat switch
        {
            BuffStat.Intelligence => status.Intelligence,
            BuffStat.Defense => status.Defense,
            BuffStat.StrengthIntelligence => (status.Strength + status.Intelligence) / 2,
            _ => status.Strength
        };
    }

    private static BattleActionInput CreateMoveAction(QuestEnemyState enemy, Move move, Target target)
    {
        return new BattleActionInput(
            ActorId: enemy.Id.Value,
            Kind: BattleActionKind.UseMove,
            MoveId: move.Id.Id,
            TargetType: move.TargetType,
            AttackRange: move.AttackRange,
            SelectedPosition: move.TargetType == TargetType.Self ? null : target.Position);
    }

    private static BattleActionInput CreateWait(QuestEnemyState enemy)
    {
        return new BattleActionInput(
            ActorId: enemy.Id.Value,
            Kind: BattleActionKind.Wait,
            MoveId: null,
            TargetType: TargetType.Self,
            AttackRange: AttackRange.Single);
    }

    // --- 状況の組み立て ---

    private static Context CreateContext(
        QuestRun run,
        QuestEnemyState enemy,
        MirrorEnemySnapshot snapshot,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemyDefinitions)
    {
        var self = CreateEnemyTarget(enemy, snapshot.Name, snapshot.Status, snapshot.MoveIds);

        // ペット出撃中はプレイヤー側が下がっているため、出撃中のペットだけを狙う。
        var opponents = run.IsPetDeploymentActive
            ? run.PetDeployment!.LivingPets
                .Select(pet =>
                {
                    var actorId = new BattleActorId(pet.ActorId);
                    return new Target(
                        actorId,
                        pet.Position,
                        pet.Status.MaxHp,
                        pet.CurrentHp,
                        new BattleActorSnapshot(actorId, "ペット", BattleSide.Ally, pet.Status, new MoveSet()),
                        new BattleActorState(actorId, pet.CurrentHp, 0, pet.Ailments, pet.Buffs));
                })
                .ToArray()
            : run.PartySnapshots
                .Join(
                    run.BattleState.PartyMembers,
                    partySnapshot => partySnapshot.ParticipantId,
                    state => state.ParticipantId,
                    (partySnapshot, state) => new { partySnapshot, state })
                .Where(x => !x.state.IsDead && !x.state.HasLeftQuest)
                .Select(x =>
                {
                    var actorId = new BattleActorId(x.partySnapshot.ParticipantId.Value);
                    return new Target(
                        actorId,
                        x.partySnapshot.StartPosition,
                        x.partySnapshot.BaseStatus.MaxHp,
                        x.state.CurrentHp,
                        new BattleActorSnapshot(actorId, x.partySnapshot.DisplayName, BattleSide.Ally, x.partySnapshot.BaseStatus, x.partySnapshot.MoveSet),
                        new BattleActorState(actorId, x.state.CurrentHp, x.state.CurrentMp, x.state.Ailments, x.state.Buffs));
                })
                .ToArray();

        // 倒れた仲間も蘇生の対象として含める。
        var friends = run.BattleState.Enemies
            .Where(other => other.Id != enemy.Id && !other.IsCaptured)
            .Select(other =>
            {
                var definition = enemyDefinitions.TryGetValue(other.EnemyDefinitionId, out var found)
                    ? found
                    : throw new InvalidOperationException($"敵定義が見つかりません。 definitionId={other.EnemyDefinitionId}");
                return CreateEnemyTarget(other, definition.Name, definition.Status, definition.MoveIds);
            })
            .ToArray();

        var allTargets = opponents.Concat(friends).Append(self).ToArray();
        var positions = allTargets
            .Select(target => new BattleActorPosition(target.ActorId, target.Position))
            .ToArray();

        return new Context(
            self,
            snapshot.Status,
            opponents,
            opponents.Select(x => x.ActorId).ToHashSet(),
            friends,
            allTargets.ToDictionary(target => target.ActorId),
            allTargets.Select(target => target.Snapshot).ToArray(),
            allTargets.Select(target => target.State).ToArray(),
            new BattleFieldContext(positions),
            PrefersBackRow: MirrorJobRowPolicy.ResolveGroup(snapshot.Job) is MirrorJobGroup.Mage or MirrorJobGroup.Ranger);
    }

    private static Target CreateEnemyTarget(QuestEnemyState enemy, string name, Status status, IReadOnlyList<MoveId> moveIds)
    {
        var actorId = new BattleActorId(enemy.Id.Value);
        return new Target(
            actorId,
            enemy.Position,
            status.MaxHp,
            enemy.CurrentHp,
            new BattleActorSnapshot(actorId, name, BattleSide.Enemy, status, CreateMoveSet(moveIds)),
            new BattleActorState(actorId, enemy.CurrentHp, enemy.CurrentMp, enemy.Ailments, enemy.Buffs));
    }

    private static MoveSet CreateMoveSet(IReadOnlyList<MoveId> moveIds)
    {
        var moveSet = new MoveSet();
        for (var i = 0; i < moveIds.Count && i < MoveSet.MaxSlots; i++)
        {
            moveSet.SetSlot(i, moveIds[i]);
        }

        return moveSet;
    }

    private sealed record Context(
        Target Self,
        Status Status,
        IReadOnlyList<Target> Opponents,
        IReadOnlySet<BattleActorId> OpponentIds,
        IReadOnlyList<Target> Friends,
        IReadOnlyDictionary<BattleActorId, Target> TargetsById,
        IReadOnlyList<BattleActorSnapshot> Snapshots,
        IReadOnlyList<BattleActorState> States,
        BattleFieldContext FieldContext,
        bool PrefersBackRow);

    private sealed record Target(
        BattleActorId ActorId,
        BattlePosition Position,
        int MaxHp,
        int CurrentHp,
        BattleActorSnapshot Snapshot,
        BattleActorState State);
}
