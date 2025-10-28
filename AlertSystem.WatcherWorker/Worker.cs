using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AlertSystem.WatcherWorker;

public class WatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WatcherService> _logger;

    public WatcherService(IServiceScopeFactory serviceScopeFactory, ILogger<WatcherService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WatcherService started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAlerts();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing pending alerts");
            }

            // Wait 5 seconds before next check
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingAlerts()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Query for alerts that need processing (StatutId = 1 and ProcessedByWorker = 0)
            var pendingAlerts = await context.Alerte
                .Where(a => a.StatutId == 1 && a.ProcessedByWorker == false)
                .Select(a => a.AlertRecordId)
                .ToListAsync();

            if (pendingAlerts.Any())
            {
                _logger.LogInformation("Found {Count} pending alerts to process", pendingAlerts.Count);

                // Atomically mark alerts as being processed
                await context.Alerte
                    .Where(a => pendingAlerts.Contains(a.AlertRecordId))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.ProcessedByWorker, true));

                // Add each alert to the processing queue
                foreach (var alertRecordId in pendingAlerts)
                {
                    var queueItem = new AlertProcessingQueue
                    {
                        AlertRecordId = alertRecordId,
                        QueuedAt = DateTime.UtcNow
                    };

                    context.AlertProcessingQueue.Add(queueItem);
                    _logger.LogDebug("Added AlertRecordId {AlertRecordId} to processing queue", alertRecordId);
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully queued {Count} alerts for processing", pendingAlerts.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending alerts");
            throw;
        }
    }
}
