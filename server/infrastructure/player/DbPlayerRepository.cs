using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.domain.player;

namespace server.infrastructure.player
{
    public class SupabasePlayerRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IPlayerRepository
    {
        public async Task<Player?> GetPlayerAsync(PlayerId id)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var entity = await dbContext.Players
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id.Value);

            if (entity is null)
            {
                return null;
            }

            return MapToDomain(entity);
        }

        public async Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var cooldownUntil = nowUtc.Add(cooldown);

            var affectedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE internal.players
                SET training_battle_count = COALESCE(training_battle_count, 0) + 1,
                    training_cooldown_until = {cooldownUntil}
                WHERE id = {id.Value}
                  AND (training_cooldown_until IS NULL OR training_cooldown_until <= {nowUtc})");

            if (affectedRows > 0)
            {
                return null;
            }

            var playerExists = await dbContext.Players
                .AsNoTracking()
                .AnyAsync(x => x.Id == id.Value);

            if (!playerExists)
            {
                throw new InvalidOperationException("プレイヤーが見つかりません。");
            }

            return await dbContext.Database.SqlQueryRaw<DateTimeOffset?>(
                "SELECT training_cooldown_until FROM internal.players WHERE id = {0}",
                id.Value)
                .FirstOrDefaultAsync();
        }

        public async Task SaveAsync(Player player)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var existing = await dbContext.Players
                .SingleOrDefaultAsync(x => x.Id == player.Id.Value);

            if (existing is null)
            {
                dbContext.Players.Add(new PlayerEntity
                {
                    Id = player.Id.Value,
                    Name = player.Name,
                    Level = player.Level,
                    Exp = player.Exp,
                    MaxHp = player.Status.MaxHp,
                    MaxMp = player.Status.MaxMp,
                    Strength = player.Status.Strength,
                    Defense = player.Status.Defense,
                    Intelligence = player.Status.Intelligence,
                    Luck = player.Status.Luck,
                    Speed = player.Status.Speed,
                });
            }
            else
            {
                existing.Name = player.Name;
                existing.Level = player.Level;
                existing.Exp = player.Exp;
                existing.MaxHp = player.Status.MaxHp;
                existing.MaxMp = player.Status.MaxMp;
                existing.Strength = player.Status.Strength;
                existing.Defense = player.Status.Defense;
                existing.Intelligence = player.Status.Intelligence;
                existing.Luck = player.Status.Luck;
                existing.Speed = player.Status.Speed;
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
            new(
                new PlayerId(entity.Id),
                entity.Name,
                level: entity.Level,
                exp: entity.Exp,
                status: new Status(
                    maxHp: entity.MaxHp,
                    maxMp: entity.MaxMp,
                    strength: entity.Strength,
                    defense: entity.Defense,
                    intelligence: entity.Intelligence,
                    luck: entity.Luck,
                    speed: entity.Speed));
    }
}
