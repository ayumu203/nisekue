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

    public async Task ApplyOutcomeAsync(PlayerId playerId, bool isWin, DateTimeOffset now)
    {
        var ratingDelta = isWin ? PetBattleConstants.WinPoints : -PetBattleConstants.LossPoints;
        var initialRating = Math.Max(PetBattleConstants.RatingFloor,
            PetBattleConstants.InitialRating + ratingDelta);
        var winDelta = isWin ? 1 : 0;
        var lossDelta = isWin ? 0 : 1;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.ExecuteSqlRawAsync(
            @"INSERT INTO internal.player_pet_battle_stats
                  (player_id, rating, wins, losses, total_battles, updated_at)
              VALUES ({0}, {1}, {2}, {3}, 1, {4})
              ON CONFLICT (player_id) DO UPDATE SET
                  rating = GREATEST({5}, internal.player_pet_battle_stats.rating + {6}),
                  wins = internal.player_pet_battle_stats.wins + {7},
                  losses = internal.player_pet_battle_stats.losses + {8},
                  total_battles = internal.player_pet_battle_stats.total_battles + 1,
                  updated_at = {4}",
            playerId.Value,
            initialRating,
            winDelta,
            lossDelta,
            now,
            PetBattleConstants.RatingFloor,
            ratingDelta,
            winDelta,
            lossDelta);
    }

    private static PlayerPetBattleStats MapToDomain(PlayerPetBattleStatsEntity entity) =>
        new(new PlayerId(entity.PlayerId), entity.Rating, entity.Wins, entity.Losses, entity.TotalBattles, entity.UpdatedAt);
}
