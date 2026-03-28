namespace server.application.training;

public class TrainingWeaponMasteryPolicy
{
    public bool ShouldIncrease(int playerLevel, int enemyLevel)
    {
        return Math.Abs(playerLevel - enemyLevel) <= ResolveAllowedLevelGap(playerLevel);
    }

    public int ResolveAllowedLevelGap(int playerLevel)
    {
        if (playerLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(playerLevel), "プレイヤーレベルは1以上である必要があります。");
        }

        return (playerLevel / 5) + 5;
    }
}
