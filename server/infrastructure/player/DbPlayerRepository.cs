using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.domain.move;
using server.domain.player;
using server.shared.constants.player;

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

            var moveEntity = await dbContext.PlayerMoves
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.PlayerId == id.Value);

            return MapToDomain(entity, moveEntity);
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

            var values = await dbContext.Database.SqlQueryRaw<DateTimeOffset?>(
                "SELECT training_cooldown_until FROM internal.players WHERE id = {0}",
                id.Value)
                .ToListAsync();

            return values.Count == 0 ? nowUtc : values[0];
        }

        public async Task<bool> UpdateNameAsync(PlayerId id, string name)
        {
            var normalized = name?.Trim() ?? string.Empty;
            if (normalized.Length is < 1 or > PlayerConstants.NameMaxLength)
            {
                throw new ArgumentException($"プレイヤー名は1文字から{PlayerConstants.NameMaxLength}文字以内です.", nameof(name));
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var affectedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE internal.players
                SET name = {normalized}
                WHERE id = {id.Value}");

            return affectedRows > 0;
        }

        public async Task SaveAsync(Player player)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var existing = await dbContext.Players
                .SingleOrDefaultAsync(x => x.Id == player.Id.Value);
            var existingMoves = await dbContext.PlayerMoves
                .SingleOrDefaultAsync(x => x.PlayerId == player.Id.Value);

            if (existing is null)
            {
                dbContext.Players.Add(new PlayerEntity
                {
                    Id = player.Id.Value,
                    Name = player.Name,
                    Job = player.Job,
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

                dbContext.PlayerMoves.Add(CreateMoveEntity(player.Id, player.MoveSet));
            }
            else
            {
                existing.Job = player.Job;
                existing.Level = player.Level;
                existing.Exp = player.Exp;
                existing.MaxHp = player.Status.MaxHp;
                existing.MaxMp = player.Status.MaxMp;
                existing.Strength = player.Status.Strength;
                existing.Defense = player.Status.Defense;
                existing.Intelligence = player.Status.Intelligence;
                existing.Luck = player.Status.Luck;
                existing.Speed = player.Status.Speed;

                if (existingMoves is null)
                {
                    dbContext.PlayerMoves.Add(CreateMoveEntity(player.Id, player.MoveSet));
                }
                else
                {
                    ApplyMoveSet(existingMoves, player.MoveSet);
                }
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

        private static Player MapToDomain(PlayerEntity entity, PlayerMoveEntity? moveEntity) =>
            new(
                new PlayerId(entity.Id),
                entity.Name,
                job: entity.Job,
                level: entity.Level,
                exp: entity.Exp,
                status: new Status(
                    maxHp: entity.MaxHp,
                    maxMp: entity.MaxMp,
                    strength: entity.Strength,
                    defense: entity.Defense,
                    intelligence: entity.Intelligence,
                    luck: entity.Luck,
                    speed: entity.Speed),
                moveSet: MapToMoveSet(moveEntity));

        private static MoveSet MapToMoveSet(PlayerMoveEntity? moveEntity)
        {
            if (moveEntity is null)
            {
                return new MoveSet();
            }

            return new MoveSet(
            [
                ToMoveId(moveEntity.MoveId1),
                ToMoveId(moveEntity.MoveId2),
                ToMoveId(moveEntity.MoveId3),
                ToMoveId(moveEntity.MoveId4),
                ToMoveId(moveEntity.MoveId5),
                ToMoveId(moveEntity.MoveId6),
                ToMoveId(moveEntity.MoveId7),
                ToMoveId(moveEntity.MoveId8),
                ToMoveId(moveEntity.MoveId9),
                ToMoveId(moveEntity.MoveId10)
            ]);
        }

        private static PlayerMoveEntity CreateMoveEntity(PlayerId playerId, MoveSet moveSet)
        {
            var entity = new PlayerMoveEntity
            {
                PlayerId = playerId.Value
            };

            ApplyMoveSet(entity, moveSet);
            return entity;
        }

        private static void ApplyMoveSet(PlayerMoveEntity entity, MoveSet moveSet)
        {
            var slots = moveSet.Slots;
            if (slots.Count != MoveSet.MaxSlots)
            {
                throw new InvalidOperationException($"MoveSet のスロット数が不正です。count={slots.Count}");
            }

            entity.MoveId1 = slots[0]?.Id;
            entity.MoveId2 = slots[1]?.Id;
            entity.MoveId3 = slots[2]?.Id;
            entity.MoveId4 = slots[3]?.Id;
            entity.MoveId5 = slots[4]?.Id;
            entity.MoveId6 = slots[5]?.Id;
            entity.MoveId7 = slots[6]?.Id;
            entity.MoveId8 = slots[7]?.Id;
            entity.MoveId9 = slots[8]?.Id;
            entity.MoveId10 = slots[9]?.Id;
        }

        private static MoveId? ToMoveId(int? value)
        {
            return value.HasValue ? new MoveId(value.Value) : null;
        }
    }
}
