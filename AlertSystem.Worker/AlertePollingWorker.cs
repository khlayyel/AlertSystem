using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AlertSystem.Worker.Models;
using AlertSystem.Worker.Services;
using AlertSystem.Service;
using Microsoft.AspNetCore.SignalR;

namespace AlertSystem.Worker
{
    public class AlertePollingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AlertePollingWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _reminderIntervalMinutes;
        private readonly int _maxReminderAttempts;

        public AlertePollingWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<AlertePollingWorker> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
            _reminderIntervalMinutes = _configuration.GetValue<int>("REMINDER__INTERVAL_MINUTES", 60);
            _maxReminderAttempts = _configuration.GetValue<int>("REMINDER__MAX_ATTEMPTS", 5);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertePollingWorker started - monitoring Alerte table directly");
            _logger.LogInformation("Reminder settings: Interval={IntervalMinutes}min, MaxAttempts={MaxAttempts}", 
                _reminderIntervalMinutes, _maxReminderAttempts);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker checking for alerts at: {time}", DateTimeOffset.Now);

                using (var scope = _scopeFactory.CreateScope())
                {
                    try
                    {
                        _logger.LogInformation("Creating service scope and resolving dependencies");
                        var alertRepository = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
                        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                        var emailTemplate = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
                        var whatsAppSender = scope.ServiceProvider.GetRequiredService<IWhatsAppSender>();
                        var whatsAppTemplate = scope.ServiceProvider.GetRequiredService<AlertSystem.Service.IWhatsAppTemplateService>();
                        var webPushNotifier = scope.ServiceProvider.GetRequiredService<IWebPushNotifier>();

                        _logger.LogInformation("Services resolved successfully, starting alert processing");
                        await ProcessUnprocessedAlerts(alertRepository, emailSender, whatsAppSender, whatsAppTemplate, webPushNotifier, emailTemplate, stoppingToken);
                        await ProcessReminderAlerts(alertRepository, emailSender, whatsAppSender, whatsAppTemplate, webPushNotifier, emailTemplate, stoppingToken);
                        _logger.LogInformation("Alert processing cycle completed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while processing alerts: {Message}", ex.Message);
                    }
                } // Scope (and scoped services like DbContext) are disposed here

                _logger.LogInformation("Waiting 1 minute before next check");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait for next cycle
            }

            _logger.LogInformation("AlertePollingWorker stopped");
        }

        private async Task ProcessUnprocessedAlerts(IAlertRepository alertRepository, IEmailSender emailSender, IWhatsAppSender whatsAppSender, AlertSystem.Service.IWhatsAppTemplateService whatsAppTemplate, IWebPushNotifier webPushNotifier, IEmailTemplateService emailTemplate, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Querying database for unprocessed alerts");
            var unprocessedAlerts = await alertRepository.GetUnprocessedAlertsAsync(cancellationToken);

            if (unprocessedAlerts.Count == 0)
            {
                _logger.LogInformation("No unprocessed alerts found");
                return; // No new alerts to process
            }

            _logger.LogInformation("Found {Count} unprocessed alerts", unprocessedAlerts.Count);

            foreach (var alert in unprocessedAlerts)
            {
                try
                {
                    _logger.LogInformation("Processing alert {AlerteId}: {Title}", alert.AlerteId, alert.TitreAlerte);
                    await ProcessSingleAlert(alert, alertRepository, emailSender, whatsAppSender, whatsAppTemplate, webPushNotifier, emailTemplate, cancellationToken);
                    _logger.LogInformation("Successfully processed alert {AlerteId}", alert.AlerteId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process alert {AlerteId}: {Message}", alert.AlerteId, ex.Message);
                    // Don't mark as processed if there was an error - will retry on next poll
                }
            }
        }

        private async Task ProcessSingleAlert(AlerteModel alert, IAlertRepository alertRepository, IEmailSender emailSender, IWhatsAppSender whatsAppSender, AlertSystem.Service.IWhatsAppTemplateService whatsAppTemplate, IWebPushNotifier webPushNotifier, IEmailTemplateService emailTemplate, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing alert {AlerteId}: {Title}", alert.AlerteId, alert.TitreAlerte);

            // Determine recipients
            var recipients = await GetRecipientsForAlert(alert, alertRepository, cancellationToken);

            if (recipients.Count == 0)
            {
                _logger.LogWarning("No active recipients found for alert {AlerteId}", alert.AlerteId);
                return;
            }

            var totalAttempts = 0;
            var totalSuccess = 0;
            var channelCount = GetChannelsForAlert(alert).Count;
            // Send to each recipient via each channel
            foreach (var recipient in recipients)
            {
                // Determine channels to use for this alert (and per-recipient)
                var channels = GetChannelsForAlert(alert);

                foreach (var channel in channels)
                {
                    try
                    {
                        // Create HistoriqueAlerte per attempted platform
                        var plateformeId = channel switch { "Email" => 1, "WhatsApp" => 2, "Desktop" => 3, _ => 0 };
                        var historiqueId = await alertRepository.CreateHistoriqueAlerteAsync(
                            alert.AlerteId,
                            recipient.UserId,
                            plateformeId,
                            recipient.Email,
                            recipient.PhoneNumber,
                            recipient.DesktopDeviceToken,
                            cancellationToken);

                        totalAttempts++;
                        await SendViaChannel(channel, recipient, alert.TitreAlerte, alert.DescriptionAlerte, emailSender, whatsAppSender, whatsAppTemplate, webPushNotifier, emailTemplate, cancellationToken);
                        totalSuccess++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send alert {AlerteId} to user {UserId} via {Channel}", 
                            alert.AlerteId, recipient.UserId, channel);
                    }
                }
            }

            _logger.LogInformation("Completed processing alert {AlerteId} for {RecipientCount} recipients via {ChannelCount} channels", 
                alert.AlerteId, recipients.Count, channelCount);

            if (totalAttempts > 0 && totalSuccess > 0)
            {
                await alertRepository.MarkAlertAsProcessedAsync(alert.AlerteId, cancellationToken);
                
                // Set initial reminder for acquittementNécessaire alerts
                if (alert.AlertTypeId == 3) // 3 = acquittementNécessaire
                {
                    await alertRepository.SetInitialReminderAsync(alert.AlerteId, _reminderIntervalMinutes, cancellationToken);
                }
            }
            else
            {
                await alertRepository.MarkAlertAsFailedAsync(alert.AlerteId, cancellationToken);
            }
        }

        private async Task<List<UserModel>> GetRecipientsForAlert(AlerteModel alert, IAlertRepository alertRepository, CancellationToken cancellationToken)
        {
            // If specific recipient is set, get only that user
            if (alert.DestinataireId.HasValue)
            {
                var specificUser = await alertRepository.GetUserByIdAsync(alert.DestinataireId.Value, cancellationToken);
                return specificUser != null ? new List<UserModel> { specificUser } : new List<UserModel>();
            }

            // Otherwise, get all active users
            return await alertRepository.GetActiveUsersAsync(cancellationToken);
        }

        private static List<string> GetChannelsForAlert(AlerteModel alert)
        {
            // If specific platform is set, use only that channel
            if (alert.PlateformeEnvoieId.HasValue)
            {
                return alert.PlateformeEnvoieId.Value switch
                {
                    1 => new List<string> { "Email" },
                    2 => new List<string> { "WhatsApp" },
                    3 => new List<string> { "Desktop" },
                    _ => new List<string> { "Email", "WhatsApp", "Desktop" }
                };
            }

            // Default: send via all channels
            return new List<string> { "Email", "WhatsApp", "Desktop" };
        }

        private async Task SendViaChannel(string channel, UserModel recipient, string title, string message, IEmailSender emailSender, IWhatsAppSender whatsAppSender, AlertSystem.Service.IWhatsAppTemplateService whatsAppTemplate, IWebPushNotifier webPushNotifier, IEmailTemplateService emailTemplate, CancellationToken cancellationToken)
        {
            switch (channel)
            {
                case "Email":
                    if (!string.IsNullOrWhiteSpace(recipient.Email))
                    {
                        // Create professional email template
                        var senderName = "Système d'Alerte";
                        var confirmUrl = $"https://your-domain.com/confirm?t={Guid.NewGuid()}"; // TODO: Implement proper confirmation URL
                        var emailHtml = emailTemplate.CreateAlertEmailTemplate(title, message, senderName, DateTime.UtcNow, confirmUrl);
                        await emailSender.SendHtmlEmailAsync(recipient.Email, $"🚨 {title}", emailHtml);
                    }
                    break;

                case "WhatsApp":
                    if (!string.IsNullOrWhiteSpace(recipient.PhoneNumber))
                    {
                        // Use the same WhatsApp template service as the main system
                        var senderName = "Système d'Alerte";
                        var confirmUrl = $"https://your-domain.com/confirm?t={Guid.NewGuid()}"; // TODO: Implement proper confirmation URL
                        await whatsAppTemplate.SendAlertTemplateAsync(recipient.PhoneNumber, title, message, senderName, confirmUrl);
                    }
                    break;

                case "Desktop":
                    await webPushNotifier.SendAsync(recipient.UserId, title, message, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown channel: {Channel}", channel);
                    break;
            }
        }

        private async Task ProcessReminderAlerts(IAlertRepository alertRepository, IEmailSender emailSender, IWhatsAppSender whatsAppSender, AlertSystem.Service.IWhatsAppTemplateService whatsAppTemplate, IWebPushNotifier webPushNotifier, IEmailTemplateService emailTemplate, CancellationToken cancellationToken)
        {
            try
            {
                var reminderAlerts = await alertRepository.GetReminderAlertsAsync(cancellationToken);
                
                if (reminderAlerts.Count == 0)
                {
                    return; // No reminders due
                }

                _logger.LogInformation("Found {Count} alerts due for reminder", reminderAlerts.Count);

                foreach (var alert in reminderAlerts)
                {
                    try
                    {
                        await ProcessReminderAlert(alert, alertRepository, emailSender, whatsAppSender, whatsAppTemplate, webPushNotifier, emailTemplate, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process reminder for alert {AlerteId}", alert.AlerteId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reminder alerts");
            }
        }

        private async Task ProcessReminderAlert(AlerteModel alert, IAlertRepository alertRepository, IEmailSender emailSender, IWhatsAppSender whatsAppSender, AlertSystem.Service.IWhatsAppTemplateService whatsAppTemplate, IWebPushNotifier webPushNotifier, IEmailTemplateService emailTemplate, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing reminder for alert {AlerteId}: {Title}", alert.AlerteId, alert.TitreAlerte);

            // Get unconfirmed recipients for this alert
            var unconfirmedRecipients = await alertRepository.GetUnconfirmedRecipientsAsync(alert.AlerteId, cancellationToken);
            if (unconfirmedRecipients.Count == 0)
            {
                _logger.LogInformation("All recipients confirmed for alert {AlerteId}, stopping reminders", alert.AlerteId);
                return;
            }

            // Get channels to use
            var channels = GetChannelsForAlert(alert);

            var totalAttempts = 0;
            var totalSuccess = 0;
            var reminderAttempts = new Dictionary<int, int>(); // Track attempts per recipient

            // Re-send to each unconfirmed recipient via each channel
            foreach (var pair in unconfirmedRecipients)
            {
                var recipientId = pair.DestinataireUserId;
                var recipient = await alertRepository.GetUserByIdAsync(recipientId, cancellationToken);
                if (recipient == null)
                {
                    _logger.LogWarning("Recipient {RecipientId} not found for reminder alert {AlerteId}", recipientId, alert.AlerteId);
                    continue;
                }

                foreach (var channel in channels)
                {
                    try
                    {
                        totalAttempts++;
                        await SendViaChannel(channel, recipient, $"[RAPPEL] {alert.TitreAlerte}", alert.DescriptionAlerte, emailSender, whatsAppSender, whatsAppTemplate, webPushNotifier, emailTemplate, cancellationToken);
                        totalSuccess++;
                        
                        // Track successful attempt for this recipient
                        if (!reminderAttempts.ContainsKey(recipientId))
                            reminderAttempts[recipientId] = 0;
                        reminderAttempts[recipientId]++;
                        
                        // Insert reminder history record
                        await alertRepository.InsertReminderHistoryAsync(
                            alert.AlerteId, 
                            pair.HistoriqueAlerteId,
                            true, 
                            reminderAttempts[recipientId], 
                            null, 
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send reminder for alert {AlerteId} to user {UserId} via {Channel}", 
                            alert.AlerteId, recipient.UserId, channel);
                        
                        // Track failed attempt for this recipient
                        if (!reminderAttempts.ContainsKey(recipientId))
                            reminderAttempts[recipientId] = 0;
                        reminderAttempts[recipientId]++;
                        
                        // Insert reminder history record for failure
                        await alertRepository.InsertReminderHistoryAsync(
                            alert.AlerteId, 
                            pair.HistoriqueAlerteId,
                            false, 
                            reminderAttempts[recipientId], 
                            ex.Message, 
                            cancellationToken);
                    }
                }
            }

            // Update reminder status
            if (totalAttempts > 0)
            {
                var shouldContinue = await alertRepository.UpdateReminderStatusAsync(
                    alert.AlerteId, 
                    totalSuccess > 0, 
                    _reminderIntervalMinutes, 
                    _maxReminderAttempts, 
                    cancellationToken);

                if (!shouldContinue)
                {
                    _logger.LogInformation("Stopping reminders for alert {AlerteId} - max attempts reached or all confirmed", alert.AlerteId);
                }
            }
        }
    }
}
