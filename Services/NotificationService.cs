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
        private readonly PushServiceClient? _pushClient;
        private readonly ILogger<NotificationService> _logger;
        private readonly IConfiguration _config;

        public NotificationService(
            ApplicationDbContext db, 
            IEmailSender emailSender, 
            IConfiguration config,
            ILogger<NotificationService> logger)
        {
            _db = db;
            _emailSender = emailSender;
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
                // TODO: Implémenter l'API WhatsApp Business
                // Pour l'instant, simulons l'envoi
                _logger.LogInformation("WhatsApp message would be sent to {Phone}: {Message}", phoneNumber, message);
                
                // Simulation d'un délai d'envoi
                await Task.Delay(100);
                
                // Pour les tests, considérons que ça marche toujours
                return true;
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

        public async Task<List<string>> SendToAllPlatformsAsync(int userId, string title, string message, string? url = null)
        {
            var successfulPlatforms = new List<string>();

            // Get user info
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return successfulPlatforms;
            }

            // Send Email
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                if (await SendEmailAsync(user.Email, title, message))
                {
                    successfulPlatforms.Add("Email");
                }
            }

            // Send WhatsApp (if phone number available)
            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                if (await SendWhatsAppAsync(user.PhoneNumber, $"{title}\n\n{message}"))
                {
                    successfulPlatforms.Add("WhatsApp");
                }
            }

            // Send Push Notification
            if (await SendPushNotificationAsync(userId, title, message, url))
            {
                successfulPlatforms.Add("Push");
            }

            _logger.LogInformation("Notifications sent to user {UserId} via platforms: {Platforms}", userId, string.Join(", ", successfulPlatforms));
            return successfulPlatforms;
        }
    }
}
