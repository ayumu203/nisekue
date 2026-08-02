namespace server.shared.constants.player;

public static class PlayerConstants
{
    public const int NameMaxLength = 20;
    public const int ImagePathMaxLength = 255;
    public const int MaxLevel = 100;
    public const int RebirthRequiredLevel = 100;
    public const int RebirthGoldCost = 100000;
    /// <summary>転生1回あたりに加算されるレベルアップ成長ボーナス（%）。</summary>
    public const int RebirthGrowthBonusPercent = 10;
    public const int RebirthInheritanceRateMin = 25;
    public const int RebirthInheritanceRateMax = 35;
}
