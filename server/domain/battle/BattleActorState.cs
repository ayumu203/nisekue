using server.domain.move;
using server.domain.move.enums;

namespace server.domain.battle;

public class BattleActorState(
    BattleActorId id,
    int currentHp,
    int currentMp,
    IEnumerable<BattleAilmentState>? ailments = null,
    IEnumerable<BattleBuffState>? buffs = null)
{
    private readonly List<BattleAilmentState> _ailments = ailments?.ToList() ?? [];
    private readonly List<BattleBuffState> _buffs = buffs?.ToList() ?? [];

    public BattleActorId Id { get; } = id;
    public int CurrentHp { get; private set; } = ValidateNonNegative(currentHp, nameof(currentHp));
    public int CurrentMp { get; private set; } = ValidateNonNegative(currentMp, nameof(currentMp));
    public bool IsDead => CurrentHp <= 0;
    public IReadOnlyList<BattleAilmentState> Ailments => _ailments;
    public IReadOnlyList<BattleBuffState> Buffs => _buffs;

    public void ReceiveDamage(int value)
    {
        CurrentHp = Math.Max(0, CurrentHp - ValidateNonNegative(value, nameof(value)));
    }

    public void RestoreHp(int value, int maxHp)
    {
        var validatedValue = ValidateNonNegative(value, nameof(value));
        var validatedMaxHp = ValidatePositive(maxHp, nameof(maxHp));
        CurrentHp = Math.Min(validatedMaxHp, CurrentHp + validatedValue);
    }

    public void ConsumeMp(int value)
    {
        CurrentMp = Math.Max(0, CurrentMp - ValidateNonNegative(value, nameof(value)));
    }

    public void ApplyAilment(BattleAilmentState ailment)
    {
        ArgumentNullException.ThrowIfNull(ailment);

        var index = _ailments.FindIndex(x => x.Type == ailment.Type);
        if (index >= 0)
        {
            var existing = _ailments[index];
            var turns = Math.Max(existing.RemainingTurns, ailment.RemainingTurns);
            _ailments[index] = new BattleAilmentState(ailment.Type, turns, ailment.TriggerDamage);
            return;
        }

        _ailments.Add(ailment);
    }

    public void ApplyBuff(BattleBuffState buff, bool canStack)
    {
        ArgumentNullException.ThrowIfNull(buff);

        if (canStack)
        {
            _buffs.Add(buff);
            return;
        }

        var index = _buffs.FindIndex(x => x.Stat == buff.Stat && x.CalculationType == buff.CalculationType);
        if (index >= 0)
        {
            _buffs[index] = buff;
            return;
        }

        _buffs.Add(buff);
    }

    public void TickTurnEnd()
    {
        ApplyAilmentTurnEndEffects();
        TickAilments();
        TickBuffs();
    }

    public bool CanAct()
    {
        return !IsDead;
    }

    private void TickAilments()
    {
        if (_ailments.Count == 0)
        {
            return;
        }

        var updated = new List<BattleAilmentState>();
        foreach (var ailment in _ailments)
        {
            var remainingTurns = ailment.RemainingTurns - 1;
            if (remainingTurns > 0)
            {
                updated.Add(new BattleAilmentState(ailment.Type, remainingTurns, ailment.TriggerDamage));
            }
        }

        _ailments.Clear();
        _ailments.AddRange(updated);
    }

    private void ApplyAilmentTurnEndEffects()
    {
        foreach (var ailment in _ailments)
        {
            if (IsDead)
            {
                return;
            }

            if (ailment.Type == AilmentType.Poison || ailment.Type == AilmentType.PoisonTrap)
            {
                var damage = Math.Max(1, CurrentHp / 10);
                ReceiveDamage(damage);
            }

            if (ailment.Type == AilmentType.DamageTrap)
            {
                ApplyTrapDamage(ailment.TriggerDamage);
            }
        }
    }

    private void ApplyTrapDamage(DamageEffect? triggerDamage)
    {
        ArgumentNullException.ThrowIfNull(triggerDamage);

        for (var i = 0; i < triggerDamage.HitCount; i++)
        {
            if (IsDead)
            {
                return;
            }

            var damage = triggerDamage.FixedValue
                + (int)Math.Round(CurrentHp * triggerDamage.PowerRate, MidpointRounding.AwayFromZero);
            ReceiveDamage(Math.Max(1, damage));
        }
    }

    private void TickBuffs()
    {
        if (_buffs.Count == 0)
        {
            return;
        }

        var updated = new List<BattleBuffState>();
        foreach (var buff in _buffs)
        {
            var remainingTurns = buff.RemainingTurns - 1;
            if (remainingTurns > 0)
            {
                updated.Add(new BattleBuffState(buff.Stat, buff.CalculationType, buff.Value, remainingTurns));
            }
        }

        _buffs.Clear();
        _buffs.AddRange(updated);
    }

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "負数は指定できません。");
        }

        return value;
    }

    private static int ValidatePositive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "0以下は指定できません。");
        }

        return value;
    }
}
