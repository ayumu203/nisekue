using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using server.application.player;
using server.domain.move;
using server.domain.player;
using static server.shared.constants.player.PlayerCacheConstants;

namespace server.infrastructure.player;

public class DbPlayerMutationService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IMemoryCache cache) : IPlayerMutationService
{
    public async Task<TResult> MutateAsync<TResult>(PlayerId playerId, Func<Player, Task<TResult>> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var tx = await dbContext.Database.BeginTransactionAsync();

        var entity = await dbContext.Players
            .FromSqlInterpolated($"SELECT * FROM internal.players WHERE id = {playerId.Value} FOR UPDATE")
            .SingleOrDefaultAsync();
        if (entity is null)
        {
            throw new KeyNotFoundException("プレイヤーが見つかりません。");
        }

        var moveEntity = await dbContext.PlayerMoves
            .SingleOrDefaultAsync(x => x.PlayerId == playerId.Value);
        var masteredJobEntities = await dbContext.PlayerMasterJobs
            .Where(x => x.PlayerId == playerId.Value)
            .ToListAsync();

        var player = MapToDomain(entity, moveEntity, masteredJobEntities);
        var result = await mutation(player);

        ApplyPlayerEntity(entity, player);
        if (moveEntity is null)
        {
            dbContext.PlayerMoves.Add(CreateMoveEntity(player.Id, player.MoveSet));
        }
        else
        {
            ApplyMoveSet(moveEntity, player.MoveSet);
        }

        dbContext.PlayerMasterJobs.RemoveRange(masteredJobEntities);
        ApplyMasteredJobs(dbContext, player, masteredJobEntities);

        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        cache.Remove(PlayerKey(playerId.Value));
        cache.Remove(AllPlayersKey);
        return result;
    }

    private static Player MapToDomain(
        PlayerEntity entity,
        PlayerMoveEntity? moveEntity,
        IReadOnlyCollection<PlayerMasterJobEntity>? masteredJobEntities = null) =>
        new(
            new PlayerId(entity.Id),
            entity.Name,
            rebirthCount: entity.RebirthCount,
            imagePath: entity.ImagePath,
            questCooldownUntil: entity.QuestCooldownUntil,
            petBattleCooldownUntil: entity.PetBattleCooldownUntil,
            job: entity.Job,
            level: entity.Level,
            exp: entity.Exp,
            jobLevel: entity.JobLevel,
            jobExp: entity.JobExp,
            gold: entity.Gold,
            status: new Status(
                maxHp: entity.MaxHp,
                maxMp: entity.MaxMp,
                strength: entity.Strength,
                defense: entity.Defense,
                intelligence: entity.Intelligence,
                luck: entity.Luck,
                speed: entity.Speed),
            moveSet: MapToMoveSet(moveEntity),
            masteredJobs: (masteredJobEntities ?? []).Select(x => x.Job).ToHashSet(),
            expMultiplierFlags: entity.ExpMultiplierFlags,
            mapUnlockFlags: entity.MapUnlockFlags,
            roadmapUnlockFlags: entity.RoadmapUnlockFlags);

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

    private static MoveId? ToMoveId(int? value) => value is null ? null : new MoveId(value.Value);

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

    private static void ApplyMasteredJobs(AppDbContext dbContext, Player player, IReadOnlyCollection<PlayerMasterJobEntity> existing)
    {
        var masteredAtByJob = existing.ToDictionary(x => x.Job, x => x.MasteredAt);
        dbContext.PlayerMasterJobs.AddRange(player.MasteredJobs.Select(job => new PlayerMasterJobEntity
        {
            PlayerId = player.Id.Value,
            Job = job,
            MasteredAt = masteredAtByJob.GetValueOrDefault(job, DateTimeOffset.UtcNow)
        }));
    }

    private static void ApplyPlayerEntity(PlayerEntity entity, Player player)
    {
        entity.Name = player.Name;
        entity.Job = player.Job;
        entity.ImagePath = player.ImagePath;
        entity.QuestCooldownUntil = player.QuestCooldownUntil;
        entity.PetBattleCooldownUntil = player.PetBattleCooldownUntil;
        entity.RebirthCount = player.RebirthCount;
        entity.Level = player.Level;
        entity.Exp = player.Exp;
        entity.JobLevel = player.JobLevel;
        entity.JobExp = player.JobExp;
        entity.Gold = player.Gold;
        entity.MaxHp = player.Status.MaxHp;
        entity.MaxMp = player.Status.MaxMp;
        entity.Strength = player.Status.Strength;
        entity.Defense = player.Status.Defense;
        entity.Intelligence = player.Status.Intelligence;
        entity.Luck = player.Status.Luck;
        entity.Speed = player.Status.Speed;
        entity.ExpMultiplierFlags = player.ExpMultiplierFlags;
        entity.MapUnlockFlags = player.MapUnlockFlags;
        entity.RoadmapUnlockFlags = player.RoadmapUnlockFlags;
    }
}
