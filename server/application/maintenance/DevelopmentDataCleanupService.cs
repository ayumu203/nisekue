using Microsoft.EntityFrameworkCore;
using server.infrastructure;

namespace server.application.maintenance;

public sealed class DevelopmentDataCleanupService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<DevelopmentDataCleanupResult> CleanupAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;

        var result = new DevelopmentDataCleanupResult(
            DeletedPlayers: 0,
            DeletedChatRooms: 0,
            DeletedChatMessages: 0,
            DeletedPlayerMoves: 0,
            DeletedPlayerMasterJobs: 0,
            DeletedPlayerEquipments: 0,
            DeletedPlayerItemStacks: 0,
            DeletedMarketListings: 0,
            DeletedMarketTradeHistories: 0,
            DeletedItemDeletionLogs: 0,
            DeletedQuestRooms: 0,
            DeletedQuestRoomAllowedPlayers: 0,
            DeletedQuestRoomParticipants: 0,
            DeletedQuestRuns: 0,
            DeletedQuestRunPartySnapshots: 0,
            DeletedQuestRunPartyMembers: 0,
            DeletedQuestRunEnemies: 0,
            DeletedQuestTurnCommands: 0,
            DeletedQuestFloorTraps: 0,
            DeletedQuestRewardSummaries: 0);

        result = result with { DeletedQuestTurnCommands = await DeleteEntitiesAsync(dbContext, dbContext.QuestTurnCommands) };
        result = result with { DeletedQuestFloorTraps = await DeleteEntitiesAsync(dbContext, dbContext.QuestFloorTraps) };
        result = result with { DeletedQuestRunEnemies = await DeleteEntitiesAsync(dbContext, dbContext.QuestRunEnemies) };
        result = result with { DeletedQuestRunPartyMembers = await DeleteEntitiesAsync(dbContext, dbContext.QuestRunPartyMembers) };
        result = result with { DeletedQuestRunPartySnapshots = await DeleteEntitiesAsync(dbContext, dbContext.QuestRunPartySnapshots) };
        result = result with { DeletedQuestRewardSummaries = await DeleteEntitiesAsync(dbContext, dbContext.QuestRewardSummaries) };
        result = result with { DeletedQuestRuns = await DeleteEntitiesAsync(dbContext, dbContext.QuestRuns) };
        result = result with { DeletedQuestRoomParticipants = await DeleteEntitiesAsync(dbContext, dbContext.QuestRoomParticipants) };
        result = result with { DeletedQuestRoomAllowedPlayers = await DeleteEntitiesAsync(dbContext, dbContext.QuestRoomAllowedPlayers) };
        result = result with { DeletedQuestRooms = await DeleteEntitiesAsync(dbContext, dbContext.QuestRooms) };

        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return result;
    }

    private static async Task<int> DeleteEntitiesAsync<TEntity>(AppDbContext dbContext, DbSet<TEntity> dbSet) where TEntity : class
    {
        if (dbContext.Database.IsRelational())
        {
            return await dbSet.ExecuteDeleteAsync();
        }

        var entities = await dbSet.ToListAsync();
        if (entities.Count == 0)
        {
            return 0;
        }

        dbSet.RemoveRange(entities);
        await dbContext.SaveChangesAsync();
        return entities.Count;
    }
}

public sealed record DevelopmentDataCleanupResult(
    int DeletedPlayers,
    int DeletedChatRooms,
    int DeletedChatMessages,
    int DeletedPlayerMoves,
    int DeletedPlayerMasterJobs,
    int DeletedPlayerEquipments,
    int DeletedPlayerItemStacks,
    int DeletedMarketListings,
    int DeletedMarketTradeHistories,
    int DeletedItemDeletionLogs,
    int DeletedQuestRooms,
    int DeletedQuestRoomAllowedPlayers,
    int DeletedQuestRoomParticipants,
    int DeletedQuestRuns,
    int DeletedQuestRunPartySnapshots,
    int DeletedQuestRunPartyMembers,
    int DeletedQuestRunEnemies,
    int DeletedQuestTurnCommands,
    int DeletedQuestFloorTraps,
    int DeletedQuestRewardSummaries);
