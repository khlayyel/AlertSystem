using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using AlertSystem.Service;
using AlertSystem.Service.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AlertSystem.Worker;

public class ConsolidatedWorkerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConsolidatedWorkerService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    public ConsolidatedWorkerService(
        IServiceScopeFactory scopeFactory,
        ILogger<ConsolidatedWorkerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ConsolidatedWorkerService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var alertSendService = scope.ServiceProvider.GetRequiredService<AlertSendService>();
                    var alertAuditService = scope.ServiceProvider.GetRequiredService<AlertAuditService>();

                    // Step 1: Query alerts to process
                    var alertsToProcess = await dbContext.Alerte
                        .Where(a => !a.ProcessedByWorker &&
                            ((a.StatutId == 1) || (a.StatutId == 4 && a.AttemptCount < 3)))
                        .OrderBy(a => a.DateCreationAlerte)
                        .ToListAsync(stoppingToken);

                    if (alertsToProcess.Any())
                    {
                        _logger.LogInformation($"Found {alertsToProcess.Count} alerts to process");
                    }

                    foreach (var alert in alertsToProcess)
                    {
                        try
                        {
                            // Step 2: Lock the alert atomically
                            var affected = await dbContext.Database.ExecuteSqlRawAsync(
                                "UPDATE dbo.Alerte SET ProcessedByWorker = 1, AttemptCount = AttemptCount + 1 WHERE AlertRecordId = {0} AND ProcessedByWorker = 0",
                                alert.AlertRecordId);

                            if (affected == 0)
                            {
                                _logger.LogWarning($"Alert {alert.AlertRecordId} was locked by another worker, skipping");
                                continue;
                            }

                            // Reload to get updated AttemptCount
                            await dbContext.Entry(alert).ReloadAsync(stoppingToken);

                            // Refresh the alert from DB
                            await dbContext.Entry(alert).ReloadAsync(stoppingToken);
                            
                            _logger.LogInformation($"Processing alert {alert.AlertRecordId}, attempt {alert.AttemptCount}");

                            // Step 3: Send the alert
                            bool sendSuccess = false;
                            try
                            {
                                // Extract recipient information from alert
                                var emails = string.IsNullOrEmpty(alert.DestinataireEmail) ? Array.Empty<string>() : new[] { alert.DestinataireEmail };
                                var phones = string.IsNullOrEmpty(alert.DestinatairePhoneNumber) ? Array.Empty<string>() : new[] { alert.DestinatairePhoneNumber };
                                var desktop = string.IsNullOrEmpty(alert.DestinataireDesktop) ? Array.Empty<string>() : new[] { alert.DestinataireDesktop };
                                
                                sendSuccess = await alertSendService.SendExistingAlertAsync(alert.AlertRecordId, emails, phones, desktop);
                            }
                            catch (Exception sendEx)
                            {
                                _logger.LogError(sendEx, $"Error sending alert {alert.AlertRecordId}");
                                sendSuccess = false;
                            }

                            // Step 4: Update status based on result
                            if (sendSuccess)
                            {
                                alert.StatutId = 2; // Envoyé
                                alert.ProcessedByWorker = true;
                                _logger.LogInformation($"Alert {alert.AlertRecordId} sent successfully");
                            }
                            else
                            {
                                if (alert.AttemptCount >= 3)
                                {
                                    alert.StatutId = 4; // Échoué
                                    alert.ProcessedByWorker = true;
                                    _logger.LogWarning($"Alert {alert.AlertRecordId} failed after 3 attempts");
                                }
                                else
                                {
                                    // Reset to allow retry
                                    alert.ProcessedByWorker = false;
                                    _logger.LogWarning($"Alert {alert.AlertRecordId} failed, will retry. Attempt {alert.AttemptCount}");
                                }
                            }

                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing alert {alert.AlertRecordId}");
                            
                            // Mark as failed if exceeded max attempts
                            if (alert.AttemptCount >= 3)
                            {
                                alert.StatutId = 4;
                                await dbContext.SaveChangesAsync(stoppingToken);
                            }
                        }
                    }

                    // After processing sends, run auto-alert watchers (single tick each)
                    try
                    {
                        var eventsWatcher = scope.ServiceProvider.GetService<AlertSystem.Worker.Watchers.EventsWatcher>();
                        if (eventsWatcher != null) await eventsWatcher.ExecuteTickAsync(stoppingToken);
                        var hrWatcher = scope.ServiceProvider.GetService<AlertSystem.Worker.Watchers.HrWatcher>();
                        if (hrWatcher != null) await hrWatcher.ExecuteTickAsync(stoppingToken);
                        var tpvWatcher = scope.ServiceProvider.GetService<AlertSystem.Worker.Watchers.TpvBillingWatcher>();
                        if (tpvWatcher != null) await tpvWatcher.ExecuteTickAsync(stoppingToken);
                        var tresWatcher = scope.ServiceProvider.GetService<AlertSystem.Worker.Watchers.TreasuryWatcher>();
                        if (tresWatcher != null) await tresWatcher.ExecuteTickAsync(stoppingToken);
                        var resaWatcher = scope.ServiceProvider.GetService<AlertSystem.Worker.Watchers.ReservationsWatcher>();
                        if (resaWatcher != null) await resaWatcher.ExecuteTickAsync(stoppingToken);
                    }
                    catch (Exception wex)
                    {
                        _logger.LogWarning(wex, "Watcher tick failed");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker loop");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("ConsolidatedWorkerService is stopping.");
    }
}

