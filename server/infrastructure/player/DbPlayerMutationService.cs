using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using server.application.player;
using server.domain.player;
using static server.shared.constants.player.PlayerCacheConstants;

namespace server.infrastructure.player;

public class DbPlayerMutationService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IMemoryCache cache) : IPlayerMutationService
{
    public async Task<TResult> MutateAsync<TResult>(PlayerId playerId, Func<Player, Task<TResult>> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var tx = await dbContext.Database.BeginTransactionAsync();

        var entity = await dbContext.Players
            .FromSqlInterpolated($"SELECT * FROM internal.players WHERE id = {playerId.Value} FOR UPDATE")
            .SingleOrDefaultAsync();
        if (entity is null)
        {
            throw new KeyNotFoundException("プレイヤーが見つかりません。");
        }

        var aggregate = await PlayerAggregatePersistence.LoadAsync(dbContext, entity);
        var result = await mutation(aggregate.Player);

        PlayerAggregatePersistence.Apply(dbContext, entity, aggregate);

        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        cache.Remove(PlayerKey(playerId.Value));
        cache.Remove(AllPlayersKey);
        return result;
    }
}
