namespace server.domain.player;

public static class RankingStatusKeys
{
    public const string MaxHp = "maxHp";
    public const string MaxMp = "maxMp";
    public const string Strength = "strength";
    public const string Defense = "defense";
    public const string Intelligence = "intelligence";
    public const string Luck = "luck";
    public const string Speed = "speed";

    public static readonly IReadOnlyList<string> All =
    [
        MaxHp,
        MaxMp,
        Strength,
        Defense,
        Intelligence,
        Luck,
        Speed
    ];
}
