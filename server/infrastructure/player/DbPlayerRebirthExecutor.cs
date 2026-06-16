using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using server.application.player;
using server.domain.player;
using static server.shared.constants.player.PlayerCacheConstants;

namespace server.infrastructure.player;

public class DbPlayerRebirthExecutor(
    IDbContextFactory<AppDbContext> dbContextFactory,
    PlayerForUpdateLockService lockService,
    IMemoryCache cache) : IPlayerRebirthExecutor
{
    public async Task<Player> ExecuteAsync(PlayerId playerId, Func<Status, Status> buildInheritedStatus)
    {
        ArgumentNullException.ThrowIfNull(buildInheritedStatus);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;

        var entity = await lockService.LockPlayerAsync(dbContext, playerId.Value);

        var aggregate = await PlayerAggregatePersistence.LoadAsync(dbContext, entity);
        var player = aggregate.Player;

        var previousStatus = player.Status;
        var inheritedStatus = buildInheritedStatus(previousStatus);

        player.Rebirth(inheritedStatus);

        PlayerAggregatePersistence.Apply(dbContext, entity, aggregate);

        dbContext.PlayerRebirthStatusHistories.Add(new PlayerRebirthStatusHistoryEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id.Value,
            RebirthCount = player.RebirthCount,
            MaxHp = previousStatus.MaxHp,
            MaxMp = previousStatus.MaxMp,
            Strength = previousStatus.Strength,
            Defense = previousStatus.Defense,
            Intelligence = previousStatus.Intelligence,
            Luck = previousStatus.Luck,
            Speed = previousStatus.Speed,
            RebirthedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        cache.Remove(PlayerKey(playerId.Value));
        cache.Remove(AllPlayersKey);
        return player;
    }
}
