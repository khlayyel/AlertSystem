using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace AlertSystem.Services
{
    public sealed class AlertCancellationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AlertCancellationService> _logger;
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _pendingAlerts = new();

        public AlertCancellationService(IServiceProvider serviceProvider, ILogger<AlertCancellationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<bool> ScheduleAlertSending(int alertId, TimeSpan delay = default)
        {
            if (delay == default) delay = TimeSpan.FromSeconds(10); // Default 10-second cancellation window

            var cts = new CancellationTokenSource();
            _pendingAlerts[alertId] = cts;

            try
            {
                // Wait for the delay period (cancellation window) - keep "Pending" status during this time
                await Task.Delay(delay, cts.Token);

                // Mark as "Sending" just before actual sending
                await UpdateAlertStatus(alertId, "Sending");

                // If not cancelled, proceed with sending
                await ProcessAlertSending(alertId);
                await UpdateAlertStatus(alertId, "Sent");
                
                _logger.LogInformation("Alert {AlertId} sent successfully after delay", alertId);
                return true;
            }
            catch (OperationCanceledException)
            {
                await UpdateAlertStatus(alertId, "Cancelled");
                _logger.LogInformation("Alert {AlertId} was cancelled during delay period", alertId);
                return false;
            }
            catch (Exception ex)
            {
                await UpdateAlertStatus(alertId, "Failed");
                _logger.LogError(ex, "Failed to send alert {AlertId}", alertId);
                return false;
            }
            finally
            {
                _pendingAlerts.TryRemove(alertId, out _);
            }
        }

        public bool CancelAlert(int alertId)
        {
            if (_pendingAlerts.TryGetValue(alertId, out var cts))
            {
                cts.Cancel();
                _logger.LogInformation("Alert {AlertId} cancellation requested", alertId);
                return true;
            }
            return false;
        }

        public bool IsAlertPending(int alertId)
        {
            return _pendingAlerts.ContainsKey(alertId);
        }

        private async Task UpdateAlertStatus(int alertId, string status)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var recipients = await dbContext.AlertRecipients
                .Where(ar => ar.AlertId == alertId)
                .ToListAsync();

            foreach (var recipient in recipients)
            {
                recipient.SendStatus = status;
                if (status == "Sent")
                {
                    recipient.LastSentAt = DateTime.UtcNow;
                    if (recipient.Alert?.AlertType == "Obligatoire")
                    {
                        recipient.NextReminderAt = DateTime.UtcNow.AddMinutes(30); // First reminder in 30 minutes
                    }
                }
            }

            await dbContext.SaveChangesAsync();
            
            // Send SignalR notification for status change
            try
            {
                var hubContext = scope.ServiceProvider.GetService<IHubContext<AlertSystem.Hubs.NotificationsHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("alertStatusChanged", alertId, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send SignalR notification for alert status change");
            }
        }

        private async Task ProcessAlertSending(int alertId)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailSender = scope.ServiceProvider.GetService<IEmailSender>();

            var alert = await dbContext.Alerts
                .Include(a => a.Recipients)
                .ThenInclude(ar => ar.User)
                .FirstOrDefaultAsync(a => a.AlertId == alertId);

            if (alert == null) return;

            foreach (var recipient in alert.Recipients)
            {
                if (recipient.User?.Email != null && emailSender != null)
                {
                    try
                    {
                        var subject = $"[{alert.AlertType}] {alert.Title}";
                        var body = $"Titre: {alert.Title}\n" +
                                  $"Type: {alert.AlertType}\n" +
                                  $"Date: {alert.CreatedAt:yyyy-MM-dd HH:mm}\n\n" +
                                  $"Message:\n{alert.Message}\n\n" +
                                  $"Ouvrir l'alerte: http://localhost:5143/AlertsCrud/Details/{alert.AlertId}";

                        await emailSender.SendAsync(recipient.User.Email, subject, body);
                        
                        // Update delivery platforms
                        var platforms = new List<string> { "Email" };
                        recipient.DeliveryPlatforms = System.Text.Json.JsonSerializer.Serialize(platforms);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email for AlertRecipient {Id}", recipient.AlertRecipientId);
                    }
                }
            }
        }
    }
}
