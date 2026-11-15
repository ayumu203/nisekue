using server.domain.player;

namespace server.application.player;

public interface IPlayerRebirthExecutor
{
    /// <param name="ignoreRequirements">true（デバッグ用）なら転生の条件（レベル・Gold）を確認せず、Gold も減らさない。</param>
    Task<Player> ExecuteAsync(PlayerId playerId, Func<Status, Status> buildInheritedStatus, bool ignoreRequirements = false);
}
