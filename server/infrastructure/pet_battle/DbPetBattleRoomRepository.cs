using Microsoft.EntityFrameworkCore;
using server.domain.battle;
using server.domain.pet;
using server.domain.pet_battle;
using server.domain.pet_battle.enums;
using server.domain.player;

namespace server.infrastructure.pet_battle;

public class DbPetBattleRoomRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IPetBattleRoomRepository
{
    public async Task<PetBattleRoom?> GetAsync(PetBattleRoomId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PetBattleRooms
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<PetBattleRoom?> GetActiveByOwnerAsync(PlayerId ownerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.PetBattleRooms
            .AsNoTracking()
            .Where(x => x.OwnerPlayerId == ownerId.Value && x.Status == (int)PetBattleRoomStatus.WaitingForStart)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task SaveAsync(PetBattleRoom room)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var exists = await dbContext.PetBattleRooms
            .AsNoTracking()
            .AnyAsync(x => x.Id == room.Id.Value);

        if (!exists)
        {
            dbContext.PetBattleRooms.Add(new PetBattleRoomEntity
            {
                Id = room.Id.Value,
                OwnerPlayerId = room.OwnerPlayerId.Value,
                OpponentPlayerId = room.OpponentPlayerId.Value,
                Status = (int)room.Status,
                Version = room.Version,
                CloseReason = (int?)room.CloseReason,
                SlotsJson = PetBattleJsonSerializer.SerializeSlots(room.Slots),
                CreatedAt = room.CreatedAt,
                ClosedAt = room.ClosedAt,
            });
        }
        else
        {
            var entity = new PetBattleRoomEntity
            {
                Id = room.Id.Value,
                OwnerPlayerId = room.OwnerPlayerId.Value,
                OpponentPlayerId = room.OpponentPlayerId.Value,
                Status = (int)room.Status,
                Version = room.Version,
                CloseReason = (int?)room.CloseReason,
                SlotsJson = PetBattleJsonSerializer.SerializeSlots(room.Slots),
                CreatedAt = room.CreatedAt,
                ClosedAt = room.ClosedAt,
            };

            dbContext.PetBattleRooms.Attach(entity);
            var entry = dbContext.Entry(entity);
            entry.Property(x => x.Version).OriginalValue = room.PersistedVersion;
            entry.Property(x => x.Status).IsModified = true;
            entry.Property(x => x.Version).IsModified = true;
            entry.Property(x => x.CloseReason).IsModified = true;
            entry.Property(x => x.SlotsJson).IsModified = true;
            entry.Property(x => x.ClosedAt).IsModified = true;
        }

        await dbContext.SaveChangesAsync();
        room.SyncVersion(room.Version);
    }

    private static PetBattleRoom MapToDomain(PetBattleRoomEntity entity)
    {
        var slots = PetBattleJsonSerializer.DeserializeSlots(entity.SlotsJson);
        return new PetBattleRoom(
            new PetBattleRoomId(entity.Id),
            new PlayerId(entity.OwnerPlayerId),
            new PlayerId(entity.OpponentPlayerId),
            (PetBattleRoomStatus)entity.Status,
            entity.Version,
            slots,
            entity.CloseReason is null ? null : (PetBattleRoomCloseReason)entity.CloseReason.Value,
            entity.ClosedAt,
            entity.CreatedAt);
    }
}
