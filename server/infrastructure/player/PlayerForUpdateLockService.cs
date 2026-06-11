using Microsoft.EntityFrameworkCore;

namespace server.infrastructure.player;

public class PlayerForUpdateLockService
{
    public async Task<PlayerEntity> LockPlayerAsync(AppDbContext dbContext, Guid playerId)
    {
        var entity = await dbContext.Players
            .FromSqlInterpolated($"SELECT * FROM internal.players WHERE id = {playerId} FOR UPDATE")
            .SingleOrDefaultAsync();
        if (entity is null)
        {
            throw new KeyNotFoundException($"プレイヤーが見つかりません。 userId={playerId}");
        }

        return entity;
    }

    public async Task<IReadOnlyDictionary<Guid, PlayerEntity>> LockPlayersAsync(AppDbContext dbContext, IEnumerable<Guid> playerIds)
    {
        var orderedIds = playerIds
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        var lockedPlayers = new Dictionary<Guid, PlayerEntity>(orderedIds.Length);

        foreach (var playerId in orderedIds)
        {
            lockedPlayers[playerId] = await LockPlayerAsync(dbContext, playerId);
        }

        return lockedPlayers;
    }
}
