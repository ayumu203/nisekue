using Microsoft.EntityFrameworkCore;
using server.domain.pet;
using server.domain.player;
using server.domain.quest;

namespace server.infrastructure.pet;

public class DbPlayerPetRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IPlayerPetRepository
{
    public async Task<IReadOnlyList<PlayerPet>> GetByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.PlayerPets
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId.Value)
            .OrderBy(x => x.CapturedAt)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<PlayerPet?> GetAsync(PlayerPetId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PlayerPets
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<PlayerPet?> GetStandbyByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PlayerPets
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId.Value && x.IsStandby)
            .OrderBy(x => x.CapturedAt)
            .FirstOrDefaultAsync();

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<int> CountByPlayerAsync(PlayerId playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.PlayerPets
            .AsNoTracking()
            .CountAsync(x => x.PlayerId == playerId.Value);
    }

    public async Task AddAsync(PlayerPet pet)
    {
        ArgumentNullException.ThrowIfNull(pet);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.PlayerPets.Add(MapToEntity(pet));
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> SetStandbyAsync(PlayerId playerId, PlayerPetId? standbyPetId, DateTimeOffset updatedAt)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        if (standbyPetId is not null)
        {
            var exists = await dbContext.PlayerPets
                .AsNoTracking()
                .AnyAsync(x => x.PlayerId == playerId.Value && x.Id == standbyPetId.Value.Value);
            if (!exists)
            {
                return false;
            }
        }

        // ix_player_pets_standby_player (player_id 単位の部分ユニークインデックス) は行ごとに即時検査されるため、
        // 1文の UPDATE で切り替えると一時的に is_standby = TRUE が2行になり制約違反になる。
        // 先に解除してから設定する2文に分ける。
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE internal.player_pets
            SET is_standby = FALSE, updated_at = {updatedAt}
            WHERE player_id = {playerId.Value} AND is_standby = TRUE
            """);

        if (standbyPetId is not null)
        {
            var standbyPetValue = standbyPetId.Value.Value;
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE internal.player_pets
                SET is_standby = TRUE, updated_at = {updatedAt}
                WHERE player_id = {playerId.Value} AND id = {standbyPetValue}
                """);
        }

        await transaction.CommitAsync();
        return true;
    }

    public async Task SaveAsync(IEnumerable<PlayerPet> pets)
    {
        ArgumentNullException.ThrowIfNull(pets);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        foreach (var pet in pets)
        {
            var existing = await dbContext.PlayerPets.SingleOrDefaultAsync(x => x.Id == pet.Id.Value);
            if (existing is null)
            {
                dbContext.PlayerPets.Add(MapToEntity(pet));
                continue;
            }

            existing.BonusMaxHp = pet.BonusStatus.MaxHp;
            existing.BonusMaxMp = pet.BonusStatus.MaxMp;
            existing.BonusStrength = pet.BonusStatus.Strength;
            existing.BonusDefense = pet.BonusStatus.Defense;
            existing.BonusIntelligence = pet.BonusStatus.Intelligence;
            existing.BonusLuck = pet.BonusStatus.Luck;
            existing.BonusSpeed = pet.BonusStatus.Speed;
            existing.IsStandby = pet.IsStandby;
            existing.UpdatedAt = pet.UpdatedAt;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(PlayerPetId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PlayerPets.SingleOrDefaultAsync(x => x.Id == id.Value);
        if (entity is null)
        {
            return;
        }

        dbContext.PlayerPets.Remove(entity);
        await dbContext.SaveChangesAsync();
    }

    private static PlayerPet MapToDomain(PlayerPetEntity entity)
    {
        return new PlayerPet(
            new PlayerPetId(entity.Id),
            new PlayerId(entity.PlayerId),
            new QuestEnemyDefinitionId(entity.EnemyDefinitionId),
            new PetBonusStatus(
                maxHp: entity.BonusMaxHp,
                maxMp: entity.BonusMaxMp,
                strength: entity.BonusStrength,
                defense: entity.BonusDefense,
                intelligence: entity.BonusIntelligence,
                luck: entity.BonusLuck,
                speed: entity.BonusSpeed),
            entity.IsStandby,
            entity.CapturedAt,
            entity.UpdatedAt);
    }

    private static PlayerPetEntity MapToEntity(PlayerPet pet)
    {
        return new PlayerPetEntity
        {
            Id = pet.Id.Value,
            PlayerId = pet.PlayerId.Value,
            EnemyDefinitionId = pet.EnemyDefinitionId.Value,
            BonusMaxHp = pet.BonusStatus.MaxHp,
            BonusMaxMp = pet.BonusStatus.MaxMp,
            BonusStrength = pet.BonusStatus.Strength,
            BonusDefense = pet.BonusStatus.Defense,
            BonusIntelligence = pet.BonusStatus.Intelligence,
            BonusLuck = pet.BonusStatus.Luck,
            BonusSpeed = pet.BonusStatus.Speed,
            IsStandby = pet.IsStandby,
            CapturedAt = pet.CapturedAt,
            UpdatedAt = pet.UpdatedAt
        };
    }
}
