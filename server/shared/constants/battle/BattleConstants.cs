namespace server.shared.constants.battle;

public static class BattleConstants
{
    public static class Critical
    {
        public const double MinChance = 0.05d;
        public const double MaxChance = 0.30d;
        public const int MaxLuckAdvantageForChance = 25;
    }

    /// <summary>範囲攻撃の威力逓減。対象が増えるほど1体あたりのダメージを下げ、全体攻撃が一方的に有利になるのを防ぐ。</summary>
    public static class AreaDamage
    {
        /// <summary>倍率 = 1 / 対象数^Exponent。0 なら逓減なし、1 なら合計ダメージが対象数によらず一定。</summary>
        public const double FalloffExponent = 0.5d;
    }
}
