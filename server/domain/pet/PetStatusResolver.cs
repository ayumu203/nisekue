using server.domain.player;
using server.domain.quest;

namespace server.domain.pet;

public static class PetStatusResolver
{
    public static Status Resolve(PlayerPet pet, QuestEnemyDefinition enemyDefinition)
    {
        ArgumentNullException.ThrowIfNull(pet);
        ArgumentNullException.ThrowIfNull(enemyDefinition);

        if (pet.EnemyDefinitionId != enemyDefinition.Id)
        {
            throw new InvalidOperationException("ペットと敵マスタが一致していません。");
        }

        var baseStatus = enemyDefinition.Status;
        var bonus = pet.BonusStatus;
        return new Status(
            maxHp: ClampedAdd(baseStatus.MaxHp, bonus.MaxHp),
            maxMp: ClampedAdd(baseStatus.MaxMp, bonus.MaxMp),
            strength: ClampedAdd(baseStatus.Strength, bonus.Strength),
            defense: ClampedAdd(baseStatus.Defense, bonus.Defense),
            intelligence: ClampedAdd(baseStatus.Intelligence, bonus.Intelligence),
            luck: ClampedAdd(baseStatus.Luck, bonus.Luck),
            speed: ClampedAdd(baseStatus.Speed, bonus.Speed),
            accuracy: baseStatus.Accuracy,
            evasion: baseStatus.Evasion,
            criticalChance: baseStatus.CriticalChance,
            damageReduction: baseStatus.DamageReduction);
    }

    private static int ClampedAdd(int left, int right)
    {
        var sum = (long)left + right;
        return sum > int.MaxValue ? int.MaxValue : (int)sum;
    }
}
