namespace server.application.ranking;

public sealed class RankingRebuildBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<RankingRebuildBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

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
                logger.LogError(ex, "Failed to rebuild rankings.");
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
        var service = scope.ServiceProvider.GetRequiredService<RankingAggregationService>();
        var result = await service.RebuildAsync(DateTimeOffset.UtcNow);
        logger.LogInformation("Ranking rebuild completed. snapshotAt={SnapshotAt}, createdEntries={CreatedEntries}", result.SnapshotAt, result.CreatedEntries);
        stoppingToken.ThrowIfCancellationRequested();
    }
}
