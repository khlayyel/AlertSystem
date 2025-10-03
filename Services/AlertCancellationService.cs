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
            var whatsApp = scope.ServiceProvider.GetService<IWhatsAppService>();
            var hubContext = scope.ServiceProvider.GetService<Microsoft.AspNetCore.SignalR.IHubContext<AlertSystem.Hubs.NotificationsHub>>();

            var alert = await dbContext.Alerts
                .Include(a => a.Recipients)
                .ThenInclude(ar => ar.User)
                .FirstOrDefaultAsync(a => a.AlertId == alertId);

            if (alert == null) return;

            // Resolve sender display name
            string senderName = await dbContext.Users.AsNoTracking()
                .Where(u => u.UserId == alert.CreatedBy)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? $"User#{alert.CreatedBy}";

            foreach (var recipient in alert.Recipients)
            {
                // Read desired platforms once per recipient
                var platforms = new List<string>();
                try { platforms = System.Text.Json.JsonSerializer.Deserialize<List<string>>(recipient.DeliveryPlatforms) ?? new List<string>(); } catch {}
                _logger.LogInformation("Alert {AlertId} -> Recipient {RecipientId}: platforms [{Platforms}]", alert.AlertId, recipient.UserId, string.Join(", ", platforms));

                // Respect selected platforms for Email
                if (recipient.User?.Email != null && emailSender != null && platforms.Any(p => string.Equals(p, "email", StringComparison.OrdinalIgnoreCase)))
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email for AlertRecipient {Id}", recipient.AlertRecipientId);
                    }
                }

                // WhatsApp after window if platform selected
                try
                {
                    if (platforms.Any(p => string.Equals(p, "whatsapp", StringComparison.OrdinalIgnoreCase)) && whatsApp != null)
                    {
                        var phone = recipient.User?.PhoneNumber;
                        if (!string.IsNullOrWhiteSpace(phone))
                        {
                            _logger.LogInformation("Attempting WhatsApp template + text to {Phone} for alert {AlertId}", phone, alert.AlertId);
                            try { if (hubContext != null) await hubContext.Clients.User(alert.CreatedBy.ToString()).SendAsync("waLog", new { type = "attempt", phone, alertId = alert.AlertId, recipientId = recipient.UserId }); } catch {}
                            // Open a customer service window with a template, then send the actual alert text
                            var okTemplate = await whatsApp.SendTemplateHelloAsync(phone);
                            try { if (hubContext != null) await hubContext.Clients.User(alert.CreatedBy.ToString()).SendAsync("waLog", new { type = "templateResult", ok = okTemplate, phone, alertId = alert.AlertId, recipientId = recipient.UserId }); } catch {}
                            var okText = await whatsApp.SendAlertAsync(phone, alert.Title, alert.Message, senderName);
                            try { if (hubContext != null) await hubContext.Clients.User(alert.CreatedBy.ToString()).SendAsync("waLog", new { type = "textResult", ok = okText, phone, alertId = alert.AlertId, recipientId = recipient.UserId }); } catch {}
                        }
                        else
                        {
                            _logger.LogWarning("Skipping WhatsApp for recipient {RecipientId}: no phone number", recipient.UserId);
                            try { if (hubContext != null) await hubContext.Clients.User(alert.CreatedBy.ToString()).SendAsync("waLog", new { type = "skip", reason = "no_phone", alertId = alert.AlertId, recipientId = recipient.UserId }); } catch {}
                        }
                    }

                    // Desktop notification via SignalR after successful wait
                    if (platforms.Any(p => string.Equals(p, "desktop", StringComparison.OrdinalIgnoreCase)) && hubContext != null)
                    {
                        _logger.LogInformation("Emitting SignalR newAlert to user {UserId} for alert {AlertId}", recipient.UserId, alert.AlertId);
                        await hubContext.Clients.User(recipient.UserId.ToString()).SendAsync("newAlert", new
                        {
                            title = alert.Title,
                            message = alert.Message,
                            alertType = alert.AlertType,
                            alertId = alert.AlertId
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send WhatsApp for AlertRecipient {Id}", recipient.AlertRecipientId);
                }
            }
        }
    }
}
