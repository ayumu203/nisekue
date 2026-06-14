namespace server.infrastructure.player;

public class PlayerRebirthStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public int RebirthCount { get; set; }
    public int MaxHp { get; set; }
    public int MaxMp { get; set; }
    public int Strength { get; set; }
    public int Defense { get; set; }
    public int Intelligence { get; set; }
    public int Luck { get; set; }
    public int Speed { get; set; }
    public DateTimeOffset RebirthedAt { get; set; }
}
