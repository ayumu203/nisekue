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
    public async Task<Player> ExecuteAsync(PlayerId playerId, Func<Status, Status> buildInheritedStatus, bool ignoreRequirements = false)
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

        player.Rebirth(inheritedStatus, ignoreRequirements);

        PlayerAggregatePersistence.Apply(dbContext, entity, aggregate);

        // 新しい転生回数以上の履歴は通常は存在しない。デバッグ機能などで転生回数と履歴が
        // 食い違ったときに残る古い履歴を消し、(プレイヤー, 転生回数) の一意制約違反を防ぐ。
        var staleHistories = await dbContext.PlayerRebirthStatusHistories
            .Where(x => x.PlayerId == player.Id.Value && x.RebirthCount >= player.RebirthCount)
            .ToListAsync();
        if (staleHistories.Count > 0)
        {
            dbContext.PlayerRebirthStatusHistories.RemoveRange(staleHistories);
            // 削除を先に反映してから追加する（同じ転生回数の行の削除と追加の順序を確定させる）
            await dbContext.SaveChangesAsync();
        }

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
