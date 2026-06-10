using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using server.domain.pet;
using server.domain.player;
using server.domain.quest;
using static server.shared.constants.player.PlayerCacheConstants;

namespace server.infrastructure.pet;

public class DbPetTrainingExecutor(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IMemoryCache cache) : IPetTrainingExecutor
{
    public async Task<PlayerPet> TrainAsync(PlayerId playerId, PlayerPetId petId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        // 同一プレイヤーの並行育成を直列化するための行ロック
        var lockedRows = await dbContext.Database
            .SqlQueryRaw<int>("SELECT gold FROM internal.players WHERE id = {0} FOR UPDATE", playerId.Value)
            .ToListAsync();
        if (lockedRows.Count == 0)
        {
            throw new KeyNotFoundException("プレイヤーが見つかりません。");
        }

        var playerEntity = await dbContext.Players.SingleOrDefaultAsync(x => x.Id == playerId.Value)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");
        var petEntity = await dbContext.PlayerPets.SingleOrDefaultAsync(x => x.Id == petId.Value && x.PlayerId == playerId.Value)
            ?? throw new KeyNotFoundException("ペットが見つかりません。");

        if (playerEntity.Gold < PetConstants.TrainingCostGold)
        {
            throw new InvalidOperationException("所持 Gold が不足しています。");
        }

        var now = DateTimeOffset.UtcNow;
        var pet = new PlayerPet(
            new PlayerPetId(petEntity.Id),
            new PlayerId(petEntity.PlayerId),
            new QuestEnemyDefinitionId(petEntity.EnemyDefinitionId),
            new PetBonusStatus(
                maxHp: petEntity.BonusMaxHp,
                maxMp: petEntity.BonusMaxMp,
                strength: petEntity.BonusStrength,
                defense: petEntity.BonusDefense,
                intelligence: petEntity.BonusIntelligence,
                luck: petEntity.BonusLuck,
                speed: petEntity.BonusSpeed),
            petEntity.IsActive,
            petEntity.CapturedAt,
            petEntity.UpdatedAt);
        var playerBaseStatus = new Status(
            maxHp: playerEntity.MaxHp,
            maxMp: playerEntity.MaxMp,
            strength: playerEntity.Strength,
            defense: playerEntity.Defense,
            intelligence: playerEntity.Intelligence,
            luck: playerEntity.Luck,
            speed: playerEntity.Speed);

        pet.Train(playerBaseStatus, now);

        playerEntity.Gold -= PetConstants.TrainingCostGold;
        petEntity.BonusMaxHp = pet.BonusStatus.MaxHp;
        petEntity.BonusMaxMp = pet.BonusStatus.MaxMp;
        petEntity.BonusStrength = pet.BonusStatus.Strength;
        petEntity.BonusDefense = pet.BonusStatus.Defense;
        petEntity.BonusIntelligence = pet.BonusStatus.Intelligence;
        petEntity.BonusLuck = pet.BonusStatus.Luck;
        petEntity.BonusSpeed = pet.BonusStatus.Speed;
        petEntity.UpdatedAt = now;

        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        cache.Remove(PlayerKey(playerId.Value));
        cache.Remove(AllPlayersKey);

        return pet;
    }
}
