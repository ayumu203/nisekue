using server.domain.player;

namespace server.application.player;

/// <summary>
/// Gold を払ってデータリセットする（転生で詰んだときの保険）。
/// </summary>
public class PlayerDataResetService(IPlayerMutationService playerMutationService)
{
    public Task<Player> ResetDataAsync(PlayerId playerId)
    {
        return playerMutationService.MutateAsync(playerId, player =>
        {
            player.ResetData(PlayerInitialState.CreateStatus(), PlayerInitialState.CreateMoveSet());
            return Task.FromResult(player);
        });
    }
}
