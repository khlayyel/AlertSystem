using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        public AlertePollingWorker(
            ILogger<AlertePollingWorker> logger,
            IAlertRepository alertRepository,
            IEmailSender emailSender,
            IWhatsAppSender whatsAppSender,
            IWebPushNotifier webPushNotifier)
        {
            _logger = logger;
            _alertRepository = alertRepository;
            _emailSender = emailSender;
            _whatsAppSender = whatsAppSender;
            _webPushNotifier = webPushNotifier;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertePollingWorker started - monitoring Alerte table directly");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessUnprocessedAlerts(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Poll every 5 seconds
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
                        await SendViaChannel(channel, recipient, alert.TitreAlerte, alert.DescriptionAlerte, cancellationToken);
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
    }
}
