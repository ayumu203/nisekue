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
        var existing = await dbContext.PetBattleRooms.SingleOrDefaultAsync(x => x.Id == room.Id.Value);

        if (existing is null)
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
            existing.Status = (int)room.Status;
            existing.Version = room.Version;
            existing.CloseReason = (int?)room.CloseReason;
            existing.SlotsJson = PetBattleJsonSerializer.SerializeSlots(room.Slots);
            existing.ClosedAt = room.ClosedAt;
        }

        await dbContext.SaveChangesAsync();
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
