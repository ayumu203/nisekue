using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using server.domain.player;
using server.shared.constants.player;
using server.shared.constants.training;
using static server.shared.constants.player.PlayerCacheConstants;

namespace server.infrastructure.player
{
    public class SupabasePlayerRepository(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IMemoryCache cache) : IPlayerRepository
    {
        private record PlayerSnapshot(
            PlayerEntity Entity,
            PlayerMoveEntity? MoveEntity,
            IReadOnlyList<PlayerMasterJobEntity> MasteredJobEntities);

        public async Task<Player?> GetPlayerAsync(PlayerId id)
        {
            var key = PlayerKey(id.Value);
            if (cache.TryGetValue(key, out PlayerSnapshot? snapshot))
            {
                return PlayerEntityMapper.MapToDomain(snapshot!.Entity, snapshot.MoveEntity, snapshot.MasteredJobEntities);
            }

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

            var masteredJobEntities = await dbContext.PlayerMasterJobs
                .AsNoTracking()
                .Where(x => x.PlayerId == id.Value)
                .ToListAsync();

            cache.Set(key, new PlayerSnapshot(entity, moveEntity, masteredJobEntities), CacheTtl);
            return PlayerEntityMapper.MapToDomain(entity, moveEntity, masteredJobEntities);
        }

        public async Task<Player?> GetPlayerWithinLevelCapAsync(PlayerId id, int maxLevel)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var entity = await dbContext.Players
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id.Value && x.Level <= maxLevel);

            if (entity is null) return null;
            return PlayerEntityMapper.MapToDomain(entity, moveEntity: null);
        }

        public async Task<IReadOnlyList<Player>> GetPvpOpponentsAsync(PlayerId excludeId, int maxLevel, int? offset = null, int? limit = null)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var query = BuildPvpOpponentsQuery(dbContext, excludeId, maxLevel);

            if (offset is > 0)
            {
                query = query.Skip(offset.Value);
            }

            if (limit is > 0)
            {
                query = query.Take(limit.Value);
            }

            var entities = await query.ToListAsync();

            return entities.Select(e => PlayerEntityMapper.MapToDomain(e, moveEntity: null)).ToArray();
        }

        public async Task<(IReadOnlyList<Player> Opponents, int TotalCount)> GetPvpOpponentsPageAsync(
            PlayerId excludeId,
            int maxLevel,
            int? offset = null,
            int? limit = null)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var baseQuery = BuildPvpOpponentsQuery(dbContext, excludeId, maxLevel);
            var totalCount = await baseQuery.CountAsync();

            var pagedQuery = baseQuery;
            if (offset is > 0)
            {
                pagedQuery = pagedQuery.Skip(offset.Value);
            }

            if (limit is > 0)
            {
                pagedQuery = pagedQuery.Take(limit.Value);
            }

            var entities = await pagedQuery.ToListAsync();
            var opponents = entities.Select(e => PlayerEntityMapper.MapToDomain(e, moveEntity: null)).ToArray();
            return (opponents, totalCount);
        }

        public async Task<int> CountPvpOpponentsAsync(PlayerId excludeId, int maxLevel)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var query = BuildPvpOpponentsQuery(dbContext, excludeId, maxLevel);
            return await query.CountAsync();
        }

        public async Task<IReadOnlyList<Player>> GetPlayersAsync(IEnumerable<PlayerId> ids)
        {
            var idValues = ids.Select(x => x.Value).Distinct().ToList();
            if (idValues.Count == 0)
            {
                return [];
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var playerEntities = await dbContext.Players
                .AsNoTracking()
                .Where(x => idValues.Contains(x.Id))
                .ToListAsync();

            return playerEntities
                .Select(entity => PlayerEntityMapper.MapToDomain(entity, moveEntity: null))
                .ToArray();
        }

        public async Task<IReadOnlyList<Player>> GetAllAsync(int? offset = null, int? limit = null)
        {
            if (offset is null && limit is null && cache.TryGetValue(AllPlayersKey, out IReadOnlyList<PlayerEntity>? cachedEntities))
            {
                return cachedEntities!.Select(e => PlayerEntityMapper.MapToDomain(e, moveEntity: null)).ToArray();
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var query = BuildPlayersQuery(dbContext);

            if (offset is > 0)
            {
                query = query.Skip(offset.Value);
            }

            if (limit is > 0)
            {
                query = query.Take(limit.Value);
            }

            var playerEntities = await query.ToListAsync();

            if (offset is null && limit is null)
            {
                cache.Set(AllPlayersKey, (IReadOnlyList<PlayerEntity>)playerEntities, CacheTtl);
            }

            return playerEntities
                .Select(entity => PlayerEntityMapper.MapToDomain(entity, moveEntity: null))
                .ToArray();
        }

        public async Task<(IReadOnlyList<Player> Players, int TotalCount)> GetPlayersPageExcludingAsync(
            PlayerId excludeId,
            int? offset = null,
            int? limit = null)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var baseQuery = BuildPlayersQuery(dbContext, excludeId);
            var totalCount = await baseQuery.CountAsync();

            var pagedQuery = baseQuery;
            if (offset is > 0)
            {
                pagedQuery = pagedQuery.Skip(offset.Value);
            }

            if (limit is > 0)
            {
                pagedQuery = pagedQuery.Take(limit.Value);
            }

            var entities = await pagedQuery.ToListAsync();
            var players = entities.Select(e => PlayerEntityMapper.MapToDomain(e, moveEntity: null)).ToArray();
            return (players, totalCount);
        }

        public async Task<int> CountAllAsync()
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Players
                .AsNoTracking()
                .CountAsync();
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
                cache.Remove(PlayerKey(id.Value));
                cache.Remove(AllPlayersKey);
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

            if (affectedRows > 0)
            {
                cache.Remove(PlayerKey(id.Value));
                cache.Remove(AllPlayersKey);
            }

            return affectedRows > 0;
        }

        private static IQueryable<PlayerEntity> BuildPvpOpponentsQuery(AppDbContext dbContext, PlayerId excludeId, int maxLevel)
        {
            var minOpponentLevel = Math.Min(TrainingConstants.Battle.MinPvpOpponentLevel, maxLevel);
            return dbContext.Players
                .AsNoTracking()
                .Where(x => x.Id != excludeId.Value
                            && x.Level >= minOpponentLevel
                            && x.Level <= maxLevel)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .AsQueryable();
        }

        private static IQueryable<PlayerEntity> BuildPlayersQuery(AppDbContext dbContext, PlayerId? excludeId = null)
        {
            var query = dbContext.Players
                .AsNoTracking()
                .AsQueryable();

            if (excludeId is not null)
            {
                query = query.Where(x => x.Id != excludeId.Value.Value);
            }

            return query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .AsQueryable();
        }

        public async Task SaveAsync(Player player)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var existing = await dbContext.Players
                .SingleOrDefaultAsync(x => x.Id == player.Id.Value);

            if (existing is null)
            {
                dbContext.Players.Add(PlayerEntityMapper.CreatePlayerEntity(player));
                dbContext.PlayerMoves.Add(PlayerEntityMapper.CreateMoveEntity(player.Id, player.MoveSet));
                PlayerEntityMapper.SyncMasteredJobs(dbContext, player, []);
            }
            else
            {
                var existingMoves = await dbContext.PlayerMoves
                    .SingleOrDefaultAsync(x => x.PlayerId == player.Id.Value);
                var existingMasteredJobs = await dbContext.PlayerMasterJobs
                    .Where(x => x.PlayerId == player.Id.Value)
                    .ToListAsync();

                PlayerEntityMapper.ApplyPlayerEntity(existing, player);

                if (existingMoves is null)
                {
                    dbContext.PlayerMoves.Add(PlayerEntityMapper.CreateMoveEntity(player.Id, player.MoveSet));
                }
                else
                {
                    PlayerEntityMapper.ApplyMoveSet(existingMoves, player.MoveSet);
                }

                PlayerEntityMapper.SyncMasteredJobs(dbContext, player, existingMasteredJobs);
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

            cache.Remove(PlayerKey(player.Id.Value));
            cache.Remove(AllPlayersKey);
        }
    }
}
