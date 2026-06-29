using TaskFlow.Application.Interfaces.Repositories;

namespace TaskFlow.ArchiveWorker.Services;

public class ArchiveWorkerService(IServiceScopeFactory scopeFactory, ILogger<ArchiveWorkerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Archive worker started");

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunArchivePassAsync(stoppingToken);
        }
    }

    private async Task RunArchivePassAsync(CancellationToken ct)
    {
        try
        {
            await using var scope       = scopeFactory.CreateAsyncScope();
            var archiveRepo             = scope.ServiceProvider.GetRequiredService<IArchiveRepository>();
            var cutoff                  = DateTime.UtcNow.AddDays(-1);
            var totalClosed             = await archiveRepo.BulkCloseExpiredDoneTasksAsync(cutoff, ct);

            if (totalClosed > 0)
                logger.LogInformation("Archive pass complete: {Count} task(s) closed", totalClosed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Archive pass failed");
        }
    }
}
