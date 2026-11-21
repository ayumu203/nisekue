namespace server.shared.constants.player;

public static class PlayerConstants
{
    public const int NameMaxLength = 20;
    public const int ImagePathMaxLength = 255;
    public const int RoleMaxLength = 20;
    /// <summary>転生していないときのレベル上限。</summary>
    public const int BaseMaxLevel = 100;
    /// <summary>転生1回あたりに上がるレベル上限。</summary>
    public const int MaxLevelIncreasePerRebirth = 2;
    public const int RebirthRequiredLevel = 100;
    /// <summary>1回目の転生に必要な Gold。n 回目の転生には n 倍が必要。</summary>
    public const int RebirthBaseGoldCost = 100000;
    /// <summary>データリセットに必要な Gold。</summary>
    public const int DataResetGoldCost = 100000;
    /// <summary>転生1回あたりに加算されるレベルアップ成長ボーナス（%）。</summary>
    public const int RebirthGrowthBonusPercent = 10;
    public const int RebirthInheritanceRateMin = 25;
    public const int RebirthInheritanceRateMax = 35;

    /// <summary>転生回数に応じたレベル上限（100 + 2 × 転生回数）。</summary>
    public static int GetMaxLevel(int rebirthCount)
    {
        var value = (long)BaseMaxLevel + (long)Math.Max(0, rebirthCount) * MaxLevelIncreasePerRebirth;
        return (int)Math.Min(int.MaxValue, value);
    }

    /// <summary>次の転生に必要な Gold（100,000 × (転生回数 + 1)）。</summary>
    public static int GetRebirthGoldCost(int rebirthCount)
    {
        var value = (long)RebirthBaseGoldCost * ((long)Math.Max(0, rebirthCount) + 1);
        return (int)Math.Min(int.MaxValue, value);
    }
}
