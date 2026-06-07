namespace server.application.maintenance;

public sealed class QuestDataCleanupBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QuestDataCleanupBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to clean up old quest data.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                return;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<QuestDataCleanupService>();
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-7);
        var result = await service.DeleteOldDataAsync(cutoffDate, stoppingToken);
        logger.LogInformation(
            "Quest data cleanup completed. cutoffAt={CutoffAt}, deletedRooms={Rooms}, deletedRuns={Runs}, deletedTurnCommands={TurnCommands}, deletedFloorTraps={FloorTraps}, deletedRunEnemies={Enemies}, deletedPartyMembers={PartyMembers}, deletedPartySnapshots={PartySnapshots}, deletedRewardSummaries={RewardSummaries}, deletedRoomParticipants={RoomParticipants}, deletedRoomAllowedPlayers={RoomAllowedPlayers}",
            cutoffDate,
            result.DeletedQuestRooms,
            result.DeletedQuestRuns,
            result.DeletedQuestTurnCommands,
            result.DeletedQuestFloorTraps,
            result.DeletedQuestRunEnemies,
            result.DeletedQuestRunPartyMembers,
            result.DeletedQuestRunPartySnapshots,
            result.DeletedQuestRewardSummaries,
            result.DeletedQuestRoomParticipants,
            result.DeletedQuestRoomAllowedPlayers);
        stoppingToken.ThrowIfCancellationRequested();
    }
}
