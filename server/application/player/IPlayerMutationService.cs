using server.domain.player;

namespace server.application.player;

public interface IPlayerMutationService
{
    Task<TResult> MutateAsync<TResult>(PlayerId playerId, Func<Player, Task<TResult>> mutation);
}
