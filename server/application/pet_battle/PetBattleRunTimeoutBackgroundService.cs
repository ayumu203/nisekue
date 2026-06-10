namespace server.application.pet_battle;

public class PetBattleRunTimeoutBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<PetBattleRunTimeoutBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var petBattleService = scope.ServiceProvider.GetRequiredService<PetBattleService>();
                await petBattleService.ProcessExpiredRunsAsync(DateTimeOffset.UtcNow);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process expired pet battle runs.");
            }
        }
    }
}
