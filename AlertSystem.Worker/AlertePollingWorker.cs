using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AlertSystem.Worker.Models;
using AlertSystem.Worker.Services;

namespace AlertSystem.Worker
{
    public class AlertePollingWorker : BackgroundService
    {
        private readonly ILogger<AlertePollingWorker> _logger;
        private readonly IAlertRepository _alertRepository;
        private readonly IEmailSender _emailSender;
        private readonly IWhatsAppSender _whatsAppSender;
        private readonly IWebPushNotifier _webPushNotifier;
        private readonly IConfiguration _configuration;
        private readonly int _reminderIntervalMinutes;
        private readonly int _maxReminderAttempts;

        public AlertePollingWorker(
            ILogger<AlertePollingWorker> logger,
            IAlertRepository alertRepository,
            IEmailSender emailSender,
            IWhatsAppSender whatsAppSender,
            IWebPushNotifier webPushNotifier,
            IConfiguration configuration)
        {
            _logger = logger;
            _alertRepository = alertRepository;
            _emailSender = emailSender;
            _whatsAppSender = whatsAppSender;
            _webPushNotifier = webPushNotifier;
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
                try
                {
                    await ProcessUnprocessedAlerts(stoppingToken);
                    await ProcessReminderAlerts(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Poll every 1 minute
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AlertePollingWorker main loop");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait longer on error
                }
            }

            _logger.LogInformation("AlertePollingWorker stopped");
        }

        private async Task ProcessUnprocessedAlerts(CancellationToken cancellationToken)
        {
            var unprocessedAlerts = await _alertRepository.GetUnprocessedAlertsAsync(cancellationToken);

            if (unprocessedAlerts.Count == 0)
            {
                return; // No new alerts to process
            }

            _logger.LogInformation("Found {Count} unprocessed alerts", unprocessedAlerts.Count);

            foreach (var alert in unprocessedAlerts)
            {
                try
                {
                    await ProcessSingleAlert(alert, cancellationToken);
                    await _alertRepository.MarkAlertAsProcessedAsync(alert.AlerteId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process alert {AlerteId}", alert.AlerteId);
                    // Don't mark as processed if there was an error - will retry on next poll
                }
            }
        }

        private async Task ProcessSingleAlert(AlerteModel alert, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing alert {AlerteId}: {Title}", alert.AlerteId, alert.TitreAlerte);

            // Determine recipients
            var recipients = await GetRecipientsForAlert(alert, cancellationToken);

            if (recipients.Count == 0)
            {
                _logger.LogWarning("No active recipients found for alert {AlerteId}", alert.AlerteId);
                return;
            }

            // Determine channels to use
            var channels = GetChannelsForAlert(alert);

            var totalAttempts = 0;
            var totalSuccess = 0;
            // Send to each recipient via each channel
            foreach (var recipient in recipients)
            {
                // Create HistoriqueAlerte entry
                await _alertRepository.CreateHistoriqueAlerteAsync(
                    alert.AlerteId, 
                    recipient.UserId, 
                    recipient.Email ?? string.Empty, 
                    recipient.PhoneNumber ?? string.Empty, 
                    recipient.DesktopDeviceToken ?? string.Empty, 
                    cancellationToken);

                // Send via each channel
                foreach (var channel in channels)
                {
                    try
                    {
                        totalAttempts++;
                        await SendViaChannel(channel, recipient, alert.TitreAlerte, alert.DescriptionAlerte, cancellationToken);
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
                alert.AlerteId, recipients.Count, channels.Count);

            if (totalAttempts > 0 && totalSuccess > 0)
            {
                await _alertRepository.MarkAlertAsProcessedAsync(alert.AlerteId, cancellationToken);
                
                // Set initial reminder for acquittementNécessaire alerts
                if (alert.AlertTypeId == 2) // Assuming 2 = acquittementNécessaire
                {
                    await _alertRepository.SetInitialReminderAsync(alert.AlerteId, _reminderIntervalMinutes, cancellationToken);
                }
            }
            else
            {
                await _alertRepository.MarkAlertAsFailedAsync(alert.AlerteId, cancellationToken);
            }
        }

        private async Task<List<UserModel>> GetRecipientsForAlert(AlerteModel alert, CancellationToken cancellationToken)
        {
            // If specific recipient is set, get only that user
            if (alert.DestinataireId.HasValue)
            {
                var specificUser = await _alertRepository.GetUserByIdAsync(alert.DestinataireId.Value, cancellationToken);
                return specificUser != null ? new List<UserModel> { specificUser } : new List<UserModel>();
            }

            // Otherwise, get all active users
            return await _alertRepository.GetActiveUsersAsync(cancellationToken);
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

        private async Task SendViaChannel(string channel, UserModel recipient, string title, string message, CancellationToken cancellationToken)
        {
            switch (channel)
            {
                case "Email":
                    if (!string.IsNullOrWhiteSpace(recipient.Email))
                    {
                        await _emailSender.SendAsync(recipient.Email, recipient.FullName, title, message, cancellationToken);
                    }
                    break;

                case "WhatsApp":
                    if (!string.IsNullOrWhiteSpace(recipient.PhoneNumber))
                    {
                        await _whatsAppSender.SendAsync(recipient.PhoneNumber, title, message, cancellationToken);
                    }
                    break;

                case "Desktop":
                    await _webPushNotifier.SendAsync(recipient.UserId, title, message, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown channel: {Channel}", channel);
                    break;
            }
        }

        private async Task ProcessReminderAlerts(CancellationToken cancellationToken)
        {
            try
            {
                var reminderAlerts = await _alertRepository.GetReminderAlertsAsync(cancellationToken);
                
                if (reminderAlerts.Count == 0)
                {
                    return; // No reminders due
                }

                _logger.LogInformation("Found {Count} alerts due for reminder", reminderAlerts.Count);

                foreach (var alert in reminderAlerts)
                {
                    try
                    {
                        await ProcessReminderAlert(alert, cancellationToken);
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

        private async Task ProcessReminderAlert(AlerteModel alert, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing reminder for alert {AlerteId}: {Title}", alert.AlerteId, alert.TitreAlerte);

            // Get recipients for this alert
            var recipients = await GetRecipientsForAlert(alert, cancellationToken);
            if (recipients.Count == 0)
            {
                _logger.LogWarning("No recipients found for reminder alert {AlerteId}", alert.AlerteId);
                return;
            }

            // Get channels to use
            var channels = GetChannelsForAlert(alert);

            var totalAttempts = 0;
            var totalSuccess = 0;

            // Re-send to each recipient via each channel
            foreach (var recipient in recipients)
            {
                foreach (var channel in channels)
                {
                    try
                    {
                        totalAttempts++;
                        await SendViaChannel(channel, recipient, $"[RAPPEL] {alert.TitreAlerte}", alert.DescriptionAlerte, cancellationToken);
                        totalSuccess++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send reminder for alert {AlerteId} to user {UserId} via {Channel}", 
                            alert.AlerteId, recipient.UserId, channel);
                    }
                }
            }

            // Update reminder status
            if (totalAttempts > 0)
            {
                var shouldContinue = await _alertRepository.UpdateReminderStatusAsync(
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
