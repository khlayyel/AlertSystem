using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Lib.Net.Http.WebPush;

namespace AlertSystem.Services
{
    public sealed class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly IWhatsAppService _whatsAppService;
        private readonly PushServiceClient? _pushClient;
        private readonly ILogger<NotificationService> _logger;
        private readonly IConfiguration _config;

        public NotificationService(
            ApplicationDbContext db, 
            IEmailSender emailSender, 
            IWhatsAppService whatsAppService,
            IConfiguration config,
            ILogger<NotificationService> logger)
        {
            _db = db;
            _emailSender = emailSender;
            _whatsAppService = whatsAppService;
            _config = config;
            _logger = logger;

            // Initialize Push client if keys are available
            var publicKey = _config["WebPush:PublicKey"];
            var privateKey = _config["WebPush:PrivateKey"];
            var subject = _config["WebPush:Subject"];
            
            if (!string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(privateKey))
            {
                _pushClient = new PushServiceClient
                {
                    DefaultAuthentication = new Lib.Net.Http.WebPush.Authentication.VapidAuthentication(publicKey, privateKey) 
                    { 
                        Subject = subject ?? "mailto:admin@example.com" 
                    }
                };
            }
        }

        // Facade method: wraps IEmailSender to provide logging and consistent return value
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                await _emailSender.SendEmailAsync(toEmail, subject, message);
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendWhatsAppAsync(string phoneNumber, string message)
        {
            try
            {
                return await _whatsAppService.SendMessageAsync(phoneNumber, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp to {Phone}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendPushNotificationAsync(int userId, string title, string message, string? url = null)
        {
            if (_pushClient == null)
            {
                _logger.LogWarning("Push client not initialized - skipping push notification for user {UserId}", userId);
                return false;
            }

            try
            {
                var subscriptions = await _db.WebPushSubscriptions
                    .Where(s => s.UserId == userId)
                    .ToListAsync();

                if (!subscriptions.Any())
                {
                    _logger.LogInformation("No push subscriptions found for user {UserId}", userId);
                    return false;
                }

                var payload = JsonSerializer.Serialize(new { title, message, url = url ?? "/" });
                var successCount = 0;

                foreach (var subscription in subscriptions)
                {
                    try
                    {
                        var pushSubscription = new PushSubscription
                        {
                            Endpoint = subscription.Endpoint,
                            Keys = new Dictionary<string, string>
                            {
                                {"p256dh", subscription.P256dh},
                                {"auth", subscription.Auth}
                            }
                        };

                        await _pushClient.RequestPushMessageDeliveryAsync(pushSubscription, new PushMessage(payload) { Topic = "alert" });
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send push to subscription {SubscriptionId}", subscription.WebPushSubscriptionId);
                    }
                }

                _logger.LogInformation("Push notifications sent: {Success}/{Total} for user {UserId}", successCount, subscriptions.Count, userId);
                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification to user {UserId}", userId);
                return false;
            }
        }

        // Sends to all available platforms for a user (Email, WhatsApp, Push) and returns list of successful platforms
        public Task<List<string>> SendToAllPlatformsAsync(int userId, string title, string message, string? url = null)
        {
            // Nouveau modèle: pas de Users locaux; cette méthode est neutralisée pour l'instant
            _logger.LogInformation("SendToAllPlatformsAsync neutralized (no local Users). Title={Title}", title);
            return Task.FromResult(new List<string>());
        }
    }
}
