using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.domain.player;
using server.infrastructure.player;

namespace server.infrastructure;

public class SupabasePlayerRepository(AppDbContext dbContext) : IPlayerRepository
{
    public async Task<Player?> GetPlayerAsync(PlayerId id)
    {
        var entity = await dbContext.Players
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value);

        if (entity is null)
        {
            return null;
        }

        return MapToDomain(entity);
    }

    public async Task SaveAsync(Player player)
    {
        var existing = await dbContext.Players
            .SingleOrDefaultAsync(x => x.Id == player.Id.Value);

        if (existing is null)
        {
            dbContext.Players.Add(new PlayerEntity
            {
                Id = player.Id.Value,
                Name = player.Name,
            });
        }
        else
        {
            existing.Name = player.Name;
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new InvalidOperationException("同じIDのプレイヤーがすでに存在します。", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("プレイヤー情報の保存に失敗しました。", ex);
        }
    }

    private static Player MapToDomain(PlayerEntity entity) =>
        new(new PlayerId(entity.Id), entity.Name);
}
