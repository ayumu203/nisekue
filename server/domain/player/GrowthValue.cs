namespace server.domain.player;

public sealed record GrowthValue(
    int MaxHp,
    int MaxMp,
    int Strength,
    int Defense,
    int Intelligence,
    int Luck,
    int Speed);
