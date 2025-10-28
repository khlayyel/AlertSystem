using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AlertSystem.SenderWorker;

public class SenderService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SenderService> _logger;

    public SenderService(IServiceScopeFactory serviceScopeFactory, ILogger<SenderService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SenderService started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueItems();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing queue items");
            }

            // Wait 2 seconds before next check
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessQueueItems()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var alertSendService = scope.ServiceProvider.GetRequiredService<AlertSendService>();
        var alertCrudService = scope.ServiceProvider.GetRequiredService<AlertCrudService>();
        var alertAuditService = scope.ServiceProvider.GetRequiredService<AlertAuditService>();

        try
        {
            // Atomically dequeue the next item from the processing queue
            var queueItem = await context.AlertProcessingQueue
                .OrderBy(q => q.QueuedAt)
                .FirstOrDefaultAsync();

            if (queueItem != null)
            {
                _logger.LogInformation("Processing AlertRecordId {AlertRecordId} from queue", queueItem.AlertRecordId);

                // Remove from queue immediately to prevent reprocessing
                context.AlertProcessingQueue.Remove(queueItem);
                await context.SaveChangesAsync();

                // Get the alert details
                var alert = await context.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.Statut)
                    .Include(a => a.PlateformeEnvoie)
                    .Include(a => a.DestinataireUser)
                    .FirstOrDefaultAsync(a => a.AlertRecordId == queueItem.AlertRecordId);

                if (alert != null)
                {
                    try
                    {
                        // For now, simulate successful sending
                        // TODO: Implement actual sending logic using AlertSendService
                        _logger.LogInformation("Simulating successful send for AlertRecordId {AlertRecordId}", alert.AlertRecordId);
                        
                        // Update status to "Envoyé" (StatutId = 2)
                        alert.StatutId = 2;
                        alert.DateLecture = DateTime.UtcNow;
                        
                        // Handle reminder logic for "Obligatoire" alerts
                        if (alert.AlertTypeId == 2) // Obligatoire
                        {
                            alert.RappelSuivant = DateTime.UtcNow.AddHours(24); // Reminder in 24 hours
                        }

                        await context.SaveChangesAsync();
                        
                        _logger.LogInformation("Successfully sent AlertRecordId {AlertRecordId}", alert.AlertRecordId);
                    }
                    catch (Exception ex)
                    {
                        // Update status to "Échoué" (StatutId = 4)
                        alert.StatutId = 4;
                        await context.SaveChangesAsync();
                        
                        _logger.LogError(ex, "Exception occurred while sending AlertRecordId {AlertRecordId}", 
                            alert.AlertRecordId);
                    }
                }
                else
                {
                    _logger.LogWarning("AlertRecordId {AlertRecordId} not found in database", queueItem.AlertRecordId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queue items");
            throw;
        }
    }
}
