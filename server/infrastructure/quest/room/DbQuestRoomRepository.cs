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
            roomEntity.CloseReason is null ? null : (QuestRoomCloseReason)roomEntity.CloseReason.Value,
            roomEntity.CreatedAt,
            roomEntity.ClosedAt);
    }

    public async Task SaveAsync(QuestRoom room)
    {
        ArgumentNullException.ThrowIfNull(room);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.QuestRooms.SingleOrDefaultAsync(x => x.Id == room.Id.Value);
        if (existing is null)
        {
            dbContext.QuestRooms.Add(new QuestRoomEntity
            {
                Id = room.Id.Value,
                OwnerPlayerId = room.OwnerId.Value,
                StageId = room.StageId.Value,
                Mode = (int)room.Mode,
                Status = (int)room.Status,
                CloseReason = room.CloseReason is null ? null : (int)room.CloseReason.Value,
                CreatedAt = room.CreatedAt,
                ClosedAt = room.ClosedAt
            });
        }
        else
        {
            existing.StageId = room.StageId.Value;
            existing.Mode = (int)room.Mode;
            existing.Status = (int)room.Status;
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
