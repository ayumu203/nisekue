namespace server.infrastructure.pet;

public class PlayerPetEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public int EnemyDefinitionId { get; set; }
    public int BonusMaxHp { get; set; }
    public int BonusMaxMp { get; set; }
    public int BonusStrength { get; set; }
    public int BonusDefense { get; set; }
    public int BonusIntelligence { get; set; }
    public int BonusLuck { get; set; }
    public int BonusSpeed { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
