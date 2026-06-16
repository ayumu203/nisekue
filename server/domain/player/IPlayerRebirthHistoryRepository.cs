namespace server.domain.player;

public interface IPlayerRebirthHistoryRepository
{
    Task<IReadOnlyList<PlayerRebirthStatusHistory>> GetByPlayerAsync(PlayerId playerId);
}
