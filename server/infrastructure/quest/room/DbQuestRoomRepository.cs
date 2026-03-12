using Microsoft.EntityFrameworkCore;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.infrastructure.quest.room;

public class DbQuestRoomRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IQuestRoomRepository
{
    public async Task<QuestRoom?> GetAsync(QuestRoomId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var roomEntity = await dbContext.QuestRooms
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value);
        if (roomEntity is null)
        {
            return null;
        }

        var participantEntities = await dbContext.QuestRoomParticipants
            .AsNoTracking()
            .Where(x => x.RoomId == id.Value)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync();

        var participants = participantEntities.Select(MapParticipant).ToArray();
        var formation = new FormationLayout(participants
            .Where(x => x.Status != ParticipantStatus.Left)
            .Select(x => x.Position));

        return new QuestRoom(
            new QuestRoomId(roomEntity.Id),
            new PlayerId(roomEntity.OwnerPlayerId),
            new QuestStageId(roomEntity.StageId),
            (QuestRoomMode)roomEntity.Mode,
            formation,
            participants,
            (QuestRoomStatus)roomEntity.Status,
            roomEntity.Version,
            roomEntity.CloseReason is null ? null : (QuestRoomCloseReason)roomEntity.CloseReason.Value,
            roomEntity.CreatedAt,
            roomEntity.ClosedAt);
    }

    public async Task<IReadOnlyList<QuestRoom>> SearchAsync(QuestRoomSearchCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var query = dbContext.QuestRooms.AsNoTracking().AsQueryable();

        if (condition.StageId is not null)
        {
            query = query.Where(x => x.StageId == condition.StageId.Value.Value);
        }

        if (condition.Mode is not null)
        {
            query = query.Where(x => x.Mode == (int)condition.Mode.Value);
        }

        if (condition.Status is not null)
        {
            query = query.Where(x => x.Status == (int)condition.Status.Value);
        }

        if (condition.OwnerPlayerId is not null)
        {
            query = query.Where(x => x.OwnerPlayerId == condition.OwnerPlayerId.Value.Value);
        }

        var roomEntities = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((condition.Page - 1) * condition.PageSize)
            .Take(condition.PageSize)
            .ToListAsync();

        if (roomEntities.Count == 0)
        {
            return [];
        }

        var roomIds = roomEntities.Select(x => x.Id).ToArray();
        var participantEntities = await dbContext.QuestRoomParticipants
            .AsNoTracking()
            .Where(x => roomIds.Contains(x.RoomId))
            .OrderBy(x => x.JoinedAt)
            .ToListAsync();

        var participantsByRoomId = participantEntities
            .GroupBy(x => x.RoomId)
            .ToDictionary(x => x.Key, x => x.Select(MapParticipant).ToArray());

        return roomEntities
            .Select(roomEntity =>
            {
                var participants = participantsByRoomId.GetValueOrDefault(roomEntity.Id) ?? [];
                var formation = new FormationLayout(participants
                    .Where(x => x.Status != ParticipantStatus.Left)
                    .Select(x => x.Position));

                return new QuestRoom(
                    new QuestRoomId(roomEntity.Id),
                    new PlayerId(roomEntity.OwnerPlayerId),
                    new QuestStageId(roomEntity.StageId),
                    (QuestRoomMode)roomEntity.Mode,
                    formation,
                    participants,
                    (QuestRoomStatus)roomEntity.Status,
                    roomEntity.Version,
                    roomEntity.CloseReason is null ? null : (QuestRoomCloseReason)roomEntity.CloseReason.Value,
                    roomEntity.CreatedAt,
                    roomEntity.ClosedAt);
            })
            .ToArray();
    }

    public async Task SaveAsync(QuestRoom room)
    {
        ArgumentNullException.ThrowIfNull(room);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.QuestRooms.SingleOrDefaultAsync(x => x.Id == room.Id.Value);
        try
        {
            if (existing is null)
            {
                existing = new QuestRoomEntity
                {
                    Id = room.Id.Value,
                    OwnerPlayerId = room.OwnerId.Value,
                    StageId = room.StageId.Value,
                    Mode = (int)room.Mode,
                    Status = (int)room.Status,
                    Version = 1,
                    CloseReason = room.CloseReason is null ? null : (int)room.CloseReason.Value,
                    CreatedAt = room.CreatedAt,
                    ClosedAt = room.ClosedAt
                };
                dbContext.QuestRooms.Add(existing);
            }
            else
            {
                dbContext.Entry(existing).Property(x => x.Version).OriginalValue = room.Version;
                existing.StageId = room.StageId.Value;
                existing.Mode = (int)room.Mode;
                existing.Status = (int)room.Status;
                existing.Version = room.Version + 1;
                existing.CloseReason = room.CloseReason is null ? null : (int)room.CloseReason.Value;
                existing.ClosedAt = room.ClosedAt;
            }

            var participants = await dbContext.QuestRoomParticipants
                .Where(x => x.RoomId == room.Id.Value)
                .ToListAsync();
            dbContext.QuestRoomParticipants.RemoveRange(participants);
            dbContext.QuestRoomParticipants.AddRange(room.Participants.Select(x => new QuestRoomParticipantEntity
            {
                Id = x.Id.Value,
                RoomId = room.Id.Value,
                ParticipantType = (int)x.Type,
                PlayerId = x.PlayerId?.Value,
                NpcTemplateId = x.NpcTemplateId?.Value,
                DisplayName = x.DisplayName,
                BattleRow = (int)x.Position.Row,
                BattleColumn = (int)x.Position.Column,
                ParticipantStatus = (int)x.Status,
                IsOwner = x.IsOwner,
                JoinedAt = x.JoinedAt,
                LastSeenAt = x.LastSeenAt,
                LeftAt = x.LeftAt
            }));

            await dbContext.SaveChangesAsync();
            room.SyncVersion(existing.Version);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("ルームが同時更新されました。最新状態を再取得してからやり直してください。", ex);
        }
    }

    private static QuestParticipant MapParticipant(QuestRoomParticipantEntity entity)
    {
        return new QuestParticipant(
            new QuestParticipantId(entity.Id),
            (ParticipantType)entity.ParticipantType,
            entity.DisplayName,
            new BattlePosition((BattleRow)entity.BattleRow, (BattleColumn)entity.BattleColumn),
            entity.JoinedAt,
            entity.IsOwner,
            entity.PlayerId is null ? null : new PlayerId(entity.PlayerId.Value),
            entity.NpcTemplateId is null ? null : new QuestNpcTemplateId(entity.NpcTemplateId.Value),
            (ParticipantStatus)entity.ParticipantStatus,
            entity.LastSeenAt,
            entity.LeftAt);
    }
}
