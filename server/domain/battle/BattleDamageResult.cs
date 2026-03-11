namespace server.domain.battle;

public class BattleDamageResult(int damage, bool isCritical)
{
    public int Damage { get; } = ValidateNonNegative(damage);
    public bool IsCritical { get; } = isCritical;

    private static int ValidateNonNegative(int damage)
    {
        if (damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), "damage に負数は指定できません。");
        }

        return damage;
    }
}
