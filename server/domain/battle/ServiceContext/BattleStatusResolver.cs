using server.domain.move.enums;
using server.domain.player;

namespace server.domain.battle;

public class BattleStatusResolver
{
    public Status BuildEffectiveStatus(BattleActorSnapshot snapshot, BattleActorState state)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(state);

        var maxHp = snapshot.BaseStatus.MaxHp;
        var maxMp = snapshot.BaseStatus.MaxMp;
        var strength = snapshot.BaseStatus.Strength;
        var defense = snapshot.BaseStatus.Defense;
        var intelligence = snapshot.BaseStatus.Intelligence;
        var luck = snapshot.BaseStatus.Luck;
        var speed = snapshot.BaseStatus.Speed;
        var accuracy = snapshot.BaseStatus.Accuracy;
        var evasion = snapshot.BaseStatus.Evasion;
        var criticalChance = snapshot.BaseStatus.CriticalChance;
        var damageReduction = snapshot.BaseStatus.DamageReduction;

        foreach (var buff in state.Buffs)
        {
            switch (buff.Stat)
            {
                case BuffStat.MaxHp:
                    maxHp = Apply(maxHp, buff.CalculationType, buff.Value, minValue: 1);
                    break;
                case BuffStat.MaxMp:
                    maxMp = Apply(maxMp, buff.CalculationType, buff.Value);
                    break;
                case BuffStat.Strength:
                    strength = Apply(strength, buff.CalculationType, buff.Value);
                    break;
                case BuffStat.Defense:
                    defense = Apply(defense, buff.CalculationType, buff.Value);
                    break;
                case BuffStat.Intelligence:
                    intelligence = Apply(intelligence, buff.CalculationType, buff.Value);
                    break;
                case BuffStat.Luck:
                    luck = Apply(luck, buff.CalculationType, buff.Value);
                    break;
                case BuffStat.Speed:
                    speed = Apply(speed, buff.CalculationType, buff.Value);
                    break;
                case BuffStat.Accuracy:
                    accuracy = Apply(accuracy, buff.CalculationType, buff.Value);
                    break;
                case BuffStat.Evasion:
                    evasion = Apply(evasion, buff.CalculationType, buff.Value);
                    break;
                case BuffStat.CriticalChance:
                    criticalChance = Apply(criticalChance, buff.CalculationType, buff.Value);
                    break;
                case BuffStat.DamageReduction:
                    damageReduction = Apply(damageReduction, buff.CalculationType, buff.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buff.Stat), $"未対応の BuffStat: {buff.Stat}");
            }
        }

        return new Status(maxHp, maxMp, strength, defense, intelligence, luck, speed, accuracy, evasion, criticalChance, damageReduction);
    }

    private static int Apply(int currentValue, BuffCalculationType calculationType, decimal value, int minValue = 0)
    {
        var resolved = calculationType switch
        {
            BuffCalculationType.Add => currentValue + value,
            BuffCalculationType.Mul => currentValue * value,
            BuffCalculationType.Set => value,
            _ => throw new ArgumentOutOfRangeException(nameof(calculationType), $"未対応の BuffCalculationType: {calculationType}")
        };

        return Math.Max(minValue, (int)Math.Round(resolved, MidpointRounding.AwayFromZero));
    }
}
