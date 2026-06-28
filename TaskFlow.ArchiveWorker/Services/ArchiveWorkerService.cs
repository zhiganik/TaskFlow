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
            await using var scope = scopeFactory.CreateAsyncScope();
            var workspacesRepo = scope.ServiceProvider.GetRequiredService<IWorkspacesRepository>();
            var archiveRepo    = scope.ServiceProvider.GetRequiredService<IArchiveRepository>();

            var workspaces = await workspacesRepo.GetAllAsync(ct);
            var active     = workspaces.Where(w => w.ArchiveAfterDays > 0).ToList();

            var totalClosed = 0;

            foreach (var ws in active)
            {
                var cutoff = DateTime.UtcNow.AddDays(-ws.ArchiveAfterDays);
                totalClosed += await archiveRepo.BulkCloseExpiredDoneTasksAsync(ws.Id, cutoff, ct);
            }

            if (totalClosed > 0)
                logger.LogInformation(
                    "Archive pass complete: {Count} task(s) closed across {Workspaces} workspace(s)",
                    totalClosed, active.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Archive pass failed");
        }
    }
}
