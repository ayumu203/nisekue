using Microsoft.AspNetCore.SignalR;

namespace server.application.quest;

public class QuestRunTimeoutBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QuestRunTimeoutBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var questRunService = scope.ServiceProvider.GetRequiredService<QuestRunService>();
                var mapper = scope.ServiceProvider.GetRequiredService<QuestResponseMapper>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<QuestRunHub>>();

                var updatedRuns = await questRunService.ProcessExpiredRunsAsync(DateTimeOffset.UtcNow);
                foreach (var run in updatedRuns)
                {
                    var payload = await mapper.MapQuestRunDetailAsync(run);
                    await hubContext.Clients.Group(run.Id.Value.ToString()).SendAsync("QuestRunUpdated", payload, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process expired quest runs.");
            }
        }
    }
}
