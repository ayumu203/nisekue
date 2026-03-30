using server.domain.move.enums;
using server.domain.player;

namespace server.domain.battle;

public class BattleDamageInput(
    BattleActorId attackerId,
    BattleActorId defenderId,
    Status attackerStatus,
    Status defenderStatus,
    int fixedPower,
    decimal powerRate,
    decimal criticalRate,
    decimal criticalChanceBonus,
    ElementType elementType,
    BuffStat attackStat)
{
    public BattleActorId AttackerId { get; } = attackerId;
    public BattleActorId DefenderId { get; } = defenderId;
    public Status AttackerStatus { get; } = attackerStatus ?? throw new ArgumentNullException(nameof(attackerStatus));
    public Status DefenderStatus { get; } = defenderStatus ?? throw new ArgumentNullException(nameof(defenderStatus));
    public int FixedPower { get; } = ValidateNonNegative(fixedPower, nameof(fixedPower));
    public decimal PowerRate { get; } = ValidateNonNegative(powerRate, nameof(powerRate));
    public decimal CriticalRate { get; } = ValidateNonNegative(criticalRate, nameof(criticalRate));
    public decimal CriticalChanceBonus { get; } = ValidateNonNegative(criticalChanceBonus, nameof(criticalChanceBonus));
    public ElementType ElementType { get; } = elementType;
    public BuffStat AttackStat { get; } = attackStat;

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "負数は指定できません。");
        }

        return value;
    }

    private static decimal ValidateNonNegative(decimal value, string paramName)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, "負数は指定できません。");
        }

        return value;
    }
}
