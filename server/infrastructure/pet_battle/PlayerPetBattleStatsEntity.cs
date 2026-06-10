namespace server.infrastructure.pet_battle;

public class PlayerPetBattleStatsEntity
{
    public Guid PlayerId { get; set; }
    public int Rating { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int TotalBattles { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
