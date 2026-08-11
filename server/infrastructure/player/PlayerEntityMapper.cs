using server.domain.move;
using server.domain.player;

namespace server.infrastructure.player;

internal static class PlayerEntityMapper
{
    public static Player MapToDomain(
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
            roadmapUnlockFlags: entity.RoadmapUnlockFlags,
            endlessBestFloor: entity.EndlessBestFloor);

    public static PlayerEntity CreatePlayerEntity(Player player)
    {
        return new PlayerEntity
        {
            Id = player.Id.Value,
            Name = player.Name,
            ImagePath = player.ImagePath,
            QuestCooldownUntil = player.QuestCooldownUntil,
            PetBattleCooldownUntil = player.PetBattleCooldownUntil,
            Job = player.Job,
            RebirthCount = player.RebirthCount,
            Level = player.Level,
            Exp = player.Exp,
            JobLevel = player.JobLevel,
            JobExp = 0,
            Gold = player.Gold,
            MaxHp = player.Status.MaxHp,
            MaxMp = player.Status.MaxMp,
            Strength = player.Status.Strength,
            Defense = player.Status.Defense,
            Intelligence = player.Status.Intelligence,
            Luck = player.Status.Luck,
            Speed = player.Status.Speed,
            ExpMultiplierFlags = player.ExpMultiplierFlags,
            MapUnlockFlags = player.MapUnlockFlags,
            RoadmapUnlockFlags = player.RoadmapUnlockFlags,
            EndlessBestFloor = player.EndlessBestFloor,
        };
    }

    public static void ApplyPlayerEntity(PlayerEntity entity, Player player)
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
        entity.JobExp = 0;
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
        entity.EndlessBestFloor = player.EndlessBestFloor;
    }

    public static PlayerMoveEntity CreateMoveEntity(PlayerId playerId, MoveSet moveSet)
    {
        var entity = new PlayerMoveEntity
        {
            PlayerId = playerId.Value
        };

        ApplyMoveSet(entity, moveSet);
        return entity;
    }

    public static void ApplyMoveSet(PlayerMoveEntity entity, MoveSet moveSet)
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

    public static void SyncMasteredJobs(
        AppDbContext dbContext,
        Player player,
        IReadOnlyCollection<PlayerMasterJobEntity> existingMasteredJobs)
    {
        var targetJobs = player.MasteredJobs;
        var existingByJob = existingMasteredJobs.ToDictionary(x => x.Job);

        var toRemove = existingMasteredJobs
            .Where(x => !targetJobs.Contains(x.Job))
            .ToArray();
        if (toRemove.Length > 0)
        {
            dbContext.PlayerMasterJobs.RemoveRange(toRemove);
        }

        var toAdd = targetJobs
            .Where(job => !existingByJob.ContainsKey(job))
            .Select(job => new PlayerMasterJobEntity
            {
                PlayerId = player.Id.Value,
                Job = job,
                MasteredAt = DateTimeOffset.UtcNow
            })
            .ToArray();
        if (toAdd.Length > 0)
        {
            dbContext.PlayerMasterJobs.AddRange(toAdd);
        }
    }

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

    private static MoveId? ToMoveId(int? value)
    {
        return value.HasValue ? new MoveId(value.Value) : null;
    }
}
