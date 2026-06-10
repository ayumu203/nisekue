using server.domain.player;

namespace server.domain.pet_battle;

public class PlayerPetBattleStats(
    PlayerId playerId,
    int rating,
    int wins,
    int losses,
    int totalBattles,
    DateTimeOffset updatedAt)
{
    public PlayerId PlayerId { get; } = playerId;
    public int Rating { get; private set; } = rating;
    public int Wins { get; private set; } = wins;
    public int Losses { get; private set; } = losses;
    public int TotalBattles { get; private set; } = totalBattles;
    public DateTimeOffset UpdatedAt { get; private set; } = updatedAt;

    public static PlayerPetBattleStats CreateInitial(PlayerId playerId, DateTimeOffset now) =>
        new(playerId, PetBattleConstants.InitialRating, 0, 0, 0, now);

    public void ApplyWin(DateTimeOffset now)
    {
        Rating = Math.Min(int.MaxValue, Rating + PetBattleConstants.WinPoints);
        Wins++;
        TotalBattles++;
        UpdatedAt = now;
    }

    public void ApplyLoss(DateTimeOffset now)
    {
        Rating = Math.Max(PetBattleConstants.RatingFloor, Rating - PetBattleConstants.LossPoints);
        Losses++;
        TotalBattles++;
        UpdatedAt = now;
    }
}
