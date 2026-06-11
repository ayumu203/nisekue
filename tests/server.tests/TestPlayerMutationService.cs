using server.application.player;
using server.domain.player;

namespace server.tests;

internal sealed class TestPlayerMutationService(IPlayerRepository playerRepository) : IPlayerMutationService
{
    public async Task<TResult> MutateAsync<TResult>(PlayerId playerId, Func<Player, Task<TResult>> mutation)
    {
        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");
        var result = await mutation(player);
        await playerRepository.SaveAsync(player);
        return result;
    }
}
