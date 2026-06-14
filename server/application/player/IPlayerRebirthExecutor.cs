using server.domain.player;

namespace server.application.player;

public interface IPlayerRebirthExecutor
{
    Task<Player> ExecuteAsync(PlayerId playerId, Func<Status, Status> buildInheritedStatus);
}
