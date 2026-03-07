namespace server.infrastructure.player;

public class PlayerEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Exp { get; set; }
    public int MaxHp { get; set; }
    public int MaxMp { get; set; }
    public int Strength { get; set; }
    public int Defense { get; set; }
    public int Intelligence { get; set; }
    public int Luck { get; set; }
    public int Speed { get; set; }
    public int TrainingBattleCount { get; set; }
    public DateTimeOffset? TrainingCooldownUntil { get; set; }
}
