using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.SignalR.Client;

namespace AlertSystem.SenderWorker;

public class SenderService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SenderService> _logger;
    private readonly IConfiguration _configuration;
    private HubConnection? _hubConnection;

    public SenderService(IServiceScopeFactory serviceScopeFactory, ILogger<SenderService> logger, IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SenderService started at: {time}", DateTimeOffset.Now);

        // Lazy init SignalR connection to WEB hub (best-effort)
        try
        {
            var baseUrl = _configuration.GetValue<string>("Web:BaseUrl") ?? "http://localhost:5185";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl.TrimEnd('/')}/hubs/notifications")
                .WithAutomaticReconnect()
                .Build();
            await _hubConnection.StartAsync(stoppingToken);
            _logger.LogInformation("SenderService: Connected to SignalR hub at {Url}", baseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SenderService: SignalR hub connection not available. Continuing without realtime.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAlerts();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing queue items");
            }

            var poll = _configuration.GetValue<int?>("Sender:PollSeconds") ?? 2;
            await Task.Delay(TimeSpan.FromSeconds(poll), stoppingToken);
        }
    }

    private async Task ProcessPendingAlerts()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var alertSendService = scope.ServiceProvider.GetRequiredService<AlertSendService>();
        var alertCrudService = scope.ServiceProvider.GetRequiredService<AlertCrudService>();
        var alertAuditService = scope.ServiceProvider.GetRequiredService<AlertAuditService>();

        try
        {
            // Pick next pending alert directly from Alerte (no intermediary queue)
            var alert = await context.Alerte
                .Where(a => a.StatutId == 1 && a.ProcessedByWorker == false)
                .OrderBy(a => a.DateCreationAlerte)
                .FirstOrDefaultAsync();

            if (alert != null)
            {
                _logger.LogInformation("Processing AlertRecordId {AlertRecordId}", alert.AlertRecordId);

                // Mark as being processed to avoid races
                alert.ProcessedByWorker = true;
                alert.AttemptCount = alert.AttemptCount + 1;
                await context.SaveChangesAsync();

                try
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

                        // Broadcast real-time update (best-effort)
                        await BroadcastStatusAsync(alert, "AlertProcessed");
                        
                        _logger.LogInformation("Successfully sent AlertRecordId {AlertRecordId}", alert.AlertRecordId);
                    }
                    catch (Exception ex)
                    {
                        // Update status to "Échoué" (StatutId = 4)
                        alert.StatutId = 4;
                        await context.SaveChangesAsync();

                        await BroadcastStatusAsync(alert, "AlertProcessed");
                        
                        _logger.LogError(ex, "Exception occurred while sending AlertRecordId {AlertRecordId}", 
                            alert.AlertRecordId);
                    }
                }
                finally
                {
                    // Allow reprocessing later if failed
                    if (alert.StatutId == 4)
                    {
                        alert.ProcessedByWorker = false;
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending alerts");
            throw;
        }
    }

    private async Task BroadcastStatusAsync(Alerte alert, string type)
    {
        try
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected) return;
            var payload = new
            {
                alertId = alert.AlertRecordId,
                groupId = alert.AlertGroupId,
                statutId = alert.StatutId,
                etatId = alert.EtatAlerteId,
                platformId = alert.PlateformeEnvoieId,
                sentAt = DateTime.UtcNow
            };
            // Targeted notifications (if clients joined groups)
            if (alert.ExpediteurId.HasValue)
                await _hubConnection.InvokeAsync("SendToUser", alert.ExpediteurId.Value.ToString(), type, payload);
            if (alert.DestinataireUserId.HasValue)
                await _hubConnection.InvokeAsync("SendToUser", Convert.ToInt32(alert.DestinataireUserId.Value).ToString(), type, payload);
            // Broadcast as fallback to ensure realtime UI even if groups weren't joined yet
            await _hubConnection.InvokeAsync("SendToAll", type, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SenderService: Failed to broadcast SignalR status for alert {Id}", alert.AlertRecordId);
        }
    }
}
