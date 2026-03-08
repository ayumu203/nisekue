using server.domain.move.enums;
using server.shared.constants.move;

namespace server.domain.move;

public class MoveEffect(
    MoveEffectId effectId,
    MoveId moveId,
    int sequence,
    MoveEffectType effectType,
    DamageEffect? damage = null,
    AilmentEffect? ailment = null,
    BuffEffect? buff = null)
{
    public MoveEffectId EffectId { get; } = effectId ?? throw new ArgumentNullException(nameof(effectId));
    public MoveId MoveId { get; } = moveId ?? throw new ArgumentNullException(nameof(moveId));
    public int Sequence { get; } = ValidateSequence(sequence);
    public MoveEffectType EffectType { get; } = effectType;
    public DamageEffect? Damage { get; } = damage;
    public AilmentEffect? Ailment { get; } = ailment;
    public BuffEffect? Buff { get; } = buff;

    public void ValidateByType()
    {
        switch (EffectType)
        {
            case MoveEffectType.Damage:
            case MoveEffectType.Heal:
                ValidateOnlyDamage();
                return;
            case MoveEffectType.Ailment:
                ValidateOnlyAilment();
                return;
            case MoveEffectType.Buff:
                ValidateOnlyBuff();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(EffectType), $"未対応の effectType: {EffectType}");
        }
    }

    private void ValidateOnlyDamage()
    {
        if (Damage is null)
        {
            throw new InvalidOperationException($"{EffectType} では Damage の指定が必要です。");
        }

        if (Ailment is not null || Buff is not null)
        {
            throw new InvalidOperationException($"{EffectType} では Ailment/Buff を同時指定できません。");
        }
    }

    private void ValidateOnlyAilment()
    {
        if (Ailment is null)
        {
            throw new InvalidOperationException("Ailment では Ailment の指定が必要です。");
        }

        if (Damage is not null || Buff is not null)
        {
            throw new InvalidOperationException("Ailment では Damage/Buff を同時指定できません。");
        }
    }

    private void ValidateOnlyBuff()
    {
        if (Buff is null)
        {
            throw new InvalidOperationException("Buff では Buff の指定が必要です。");
        }

        if (Damage is not null || Ailment is not null)
        {
            throw new InvalidOperationException("Buff では Damage/Ailment を同時指定できません。");
        }
    }

    private static int ValidateSequence(int sequence)
    {
        if (sequence < MoveConstants.Constraints.MinSequence)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "sequence は1以上である必要があります。");
        }

        return sequence;
    }
}
