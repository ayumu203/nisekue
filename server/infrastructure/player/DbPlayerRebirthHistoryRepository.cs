using Microsoft.EntityFrameworkCore;
using server.domain.player;

namespace server.infrastructure.player;

public class DbPlayerRebirthHistoryRepository(
    IDbContextFactory<AppDbContext> dbContextFactory) : IPlayerRebirthHistoryRepository
{
    public async Task<IReadOnlyList<PlayerRebirthStatusHistory>> GetByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.PlayerRebirthStatusHistories
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId.Value)
            .OrderBy(x => x.RebirthCount)
            .ToListAsync();

        return entities
            .Select(x => new PlayerRebirthStatusHistory(
                new PlayerId(x.PlayerId),
                x.RebirthCount,
                new Status(
                    maxHp: x.MaxHp,
                    maxMp: x.MaxMp,
                    strength: x.Strength,
                    defense: x.Defense,
                    intelligence: x.Intelligence,
                    luck: x.Luck,
                    speed: x.Speed),
                x.RebirthedAt))
            .ToList();
    }
}
