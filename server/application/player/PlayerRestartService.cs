using server.domain.player;

namespace server.application.player;

/// <summary>
/// Gold を払って初期状態からやり直す（転生で詰んだときの保険）。
/// </summary>
public class PlayerRestartService(IPlayerMutationService playerMutationService)
{
    public Task<Player> RestartAsync(PlayerId playerId)
    {
        return playerMutationService.MutateAsync(playerId, player =>
        {
            player.Restart(PlayerInitialState.CreateStatus(), PlayerInitialState.CreateMoveSet());
            return Task.FromResult(player);
        });
    }
}
