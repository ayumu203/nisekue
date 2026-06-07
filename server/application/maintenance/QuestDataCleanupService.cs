using Microsoft.EntityFrameworkCore;
using server.domain.quest.enums;
using server.infrastructure;

namespace server.application.maintenance;

public sealed class QuestDataCleanupService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<QuestDataCleanupResult> DeleteOldDataAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var oldRoomIds = await dbContext.QuestRooms
            .Where(x => x.Status == (int)QuestRoomStatus.Closed && x.ClosedAt != null && x.ClosedAt <= cutoffDate)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (oldRoomIds.Count == 0)
        {
            return new QuestDataCleanupResult(
                DeletedQuestTurnCommands: 0,
                DeletedQuestFloorTraps: 0,
                DeletedQuestRunEnemies: 0,
                DeletedQuestRunPartyMembers: 0,
                DeletedQuestRunPartySnapshots: 0,
                DeletedQuestRewardSummaries: 0,
                DeletedQuestRuns: 0,
                DeletedQuestRoomParticipants: 0,
                DeletedQuestRoomAllowedPlayers: 0,
                DeletedQuestRooms: 0);
        }

        var runIds = await dbContext.QuestRuns
            .Where(x => oldRoomIds.Contains(x.RoomId))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var deletedTurnCommands = 0;
        var deletedFloorTraps = 0;
        var deletedRunEnemies = 0;
        var deletedPartyMembers = 0;
        var deletedPartySnapshots = 0;
        var deletedRewardSummaries = 0;
        var deletedRuns = 0;

        if (runIds.Count > 0)
        {
            deletedTurnCommands = await dbContext.QuestTurnCommands
                .Where(x => runIds.Contains(x.RunId))
                .ExecuteDeleteAsync(cancellationToken);
            deletedFloorTraps = await dbContext.QuestFloorTraps
                .Where(x => runIds.Contains(x.RunId))
                .ExecuteDeleteAsync(cancellationToken);
            deletedRunEnemies = await dbContext.QuestRunEnemies
                .Where(x => runIds.Contains(x.RunId))
                .ExecuteDeleteAsync(cancellationToken);
            deletedPartyMembers = await dbContext.QuestRunPartyMembers
                .Where(x => runIds.Contains(x.RunId))
                .ExecuteDeleteAsync(cancellationToken);
            deletedPartySnapshots = await dbContext.QuestRunPartySnapshots
                .Where(x => runIds.Contains(x.RunId))
                .ExecuteDeleteAsync(cancellationToken);
            deletedRewardSummaries = await dbContext.QuestRewardSummaries
                .Where(x => runIds.Contains(x.RunId))
                .ExecuteDeleteAsync(cancellationToken);
            deletedRuns = await dbContext.QuestRuns
                .Where(x => runIds.Contains(x.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var deletedRoomParticipants = await dbContext.QuestRoomParticipants
            .Where(x => oldRoomIds.Contains(x.RoomId))
            .ExecuteDeleteAsync(cancellationToken);
        var deletedRoomAllowedPlayers = await dbContext.QuestRoomAllowedPlayers
            .Where(x => oldRoomIds.Contains(x.RoomId))
            .ExecuteDeleteAsync(cancellationToken);
        var deletedRooms = await dbContext.QuestRooms
            .Where(x => oldRoomIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new QuestDataCleanupResult(
            deletedTurnCommands,
            deletedFloorTraps,
            deletedRunEnemies,
            deletedPartyMembers,
            deletedPartySnapshots,
            deletedRewardSummaries,
            deletedRuns,
            deletedRoomParticipants,
            deletedRoomAllowedPlayers,
            deletedRooms);
    }
}

public sealed record QuestDataCleanupResult(
    int DeletedQuestTurnCommands,
    int DeletedQuestFloorTraps,
    int DeletedQuestRunEnemies,
    int DeletedQuestRunPartyMembers,
    int DeletedQuestRunPartySnapshots,
    int DeletedQuestRewardSummaries,
    int DeletedQuestRuns,
    int DeletedQuestRoomParticipants,
    int DeletedQuestRoomAllowedPlayers,
    int DeletedQuestRooms);
