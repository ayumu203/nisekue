using Microsoft.EntityFrameworkCore;
using server.domain.pet_battle;
using server.domain.player;

namespace server.infrastructure.pet_battle;

public class DbPlayerPetBattleStatsRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IPlayerPetBattleStatsRepository
{
    public async Task<PlayerPetBattleStats?> GetByPlayerIdAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PlayerPetBattleStats
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.PlayerId == playerId.Value);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task UpsertAsync(PlayerPetBattleStats stats)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.PlayerPetBattleStats.SingleOrDefaultAsync(x => x.PlayerId == stats.PlayerId.Value);

        if (existing is null)
        {
            dbContext.PlayerPetBattleStats.Add(new PlayerPetBattleStatsEntity
            {
                PlayerId = stats.PlayerId.Value,
                Rating = stats.Rating,
                Wins = stats.Wins,
                Losses = stats.Losses,
                TotalBattles = stats.TotalBattles,
                UpdatedAt = stats.UpdatedAt,
            });
        }
        else
        {
            existing.Rating = stats.Rating;
            existing.Wins = stats.Wins;
            existing.Losses = stats.Losses;
            existing.TotalBattles = stats.TotalBattles;
            existing.UpdatedAt = stats.UpdatedAt;
        }

        await dbContext.SaveChangesAsync();
    }

    private static PlayerPetBattleStats MapToDomain(PlayerPetBattleStatsEntity entity) =>
        new(new PlayerId(entity.PlayerId), entity.Rating, entity.Wins, entity.Losses, entity.TotalBattles, entity.UpdatedAt);
}
