using WebPush;
using System.Text.Json;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Services
{
    public interface IWebPushService
    {
        Task<bool> SendNotificationAsync(string deviceToken, string title, string message);
        Task<string> SubscribeUserAsync(int userId, string endpoint, string p256dh, string auth);
        Task<bool> UnsubscribeUserAsync(int userId, string endpoint);
        Task<List<string>> GetUserDeviceTokensAsync(int userId);
    }

    public sealed class WebPushService : IWebPushService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WebPushService> _logger;
        private readonly WebPushClient _webPushClient;
        private readonly VapidDetails _vapidDetails;

        public WebPushService(ApplicationDbContext db, ILogger<WebPushService> logger, IConfiguration config)
        {
            _db = db;
            _logger = logger;
            
            // Configuration VAPID (clés pour Web Push)
            var vapidSubject = config["WebPush:Subject"] ?? "mailto:admin@alertsystem.com";
            var vapidPublicKey = config["WebPush:PublicKey"] ?? GenerateVapidKeys().PublicKey;
            var vapidPrivateKey = config["WebPush:PrivateKey"] ?? GenerateVapidKeys().PrivateKey;
            
            _vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
            _webPushClient = new WebPushClient();
            
            _logger.LogInformation("WebPushService initialized with VAPID keys");
        }

        public async Task<bool> SendNotificationAsync(string deviceToken, string title, string message)
        {
            try
            {
                // Chercher la subscription par device token
                var subscription = await _db.WebPushSubscriptions
                    .FirstOrDefaultAsync(s => s.Endpoint == deviceToken);

                if (subscription == null)
                {
                    _logger.LogWarning("No subscription found for device token: {DeviceToken}", deviceToken);
                    return false;
                }

                // Créer le payload de notification
                var payload = JsonSerializer.Serialize(new
                {
                    title = title,
                    body = message,
                    icon = "/icon-192x192.png",
                    badge = "/badge-72x72.png",
                    tag = "alert-notification",
                    requireInteraction = true,
                    actions = new[]
                    {
                        new { action = "view", title = "Voir", icon = "/action-view.png" },
                        new { action = "dismiss", title = "Ignorer", icon = "/action-dismiss.png" }
                    },
                    data = new
                    {
                        url = "/Dashboard",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    }
                });

                // Créer l'objet PushSubscription
                var pushSubscription = new PushSubscription(
                    subscription.Endpoint,
                    subscription.P256dh,
                    subscription.Auth
                );

                // Envoyer la notification
                await _webPushClient.SendNotificationAsync(pushSubscription, payload, _vapidDetails);
                
                _logger.LogInformation("Web push notification sent successfully to {DeviceToken}", deviceToken);
                return true;
            }
            catch (WebPushException ex)
            {
                _logger.LogError(ex, "WebPush error sending notification to {DeviceToken}: {StatusCode} - {Message}", 
                    deviceToken, ex.StatusCode, ex.Message);
                
                // Si l'endpoint n'est plus valide, supprimer la subscription
                if (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    await RemoveInvalidSubscriptionAsync(deviceToken);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending web push notification to {DeviceToken}", deviceToken);
                return false;
            }
        }

        public async Task<string> SubscribeUserAsync(int userId, string endpoint, string p256dh, string auth)
        {
            try
            {
                // Vérifier si la subscription existe déjà
                var existingSubscription = await _db.WebPushSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

                if (existingSubscription != null)
                {
                    // Mettre à jour les clés si nécessaire
                    existingSubscription.P256dh = p256dh;
                    existingSubscription.Auth = auth;
                    await _db.SaveChangesAsync();
                    
                    _logger.LogInformation("Updated existing web push subscription for user {UserId}", userId);
                    return endpoint;
                }

                // Créer nouvelle subscription
                var subscription = new Models.Entities.WebPushSubscription
                {
                    UserId = userId,
                    Endpoint = endpoint,
                    P256dh = p256dh,
                    Auth = auth,
                    CreatedAt = DateTime.UtcNow
                };

                _db.WebPushSubscriptions.Add(subscription);
                await _db.SaveChangesAsync();

                // Mettre à jour le device token de l'utilisateur
                var user = await _db.Users.FindAsync(userId);
                if (user != null)
                {
                    user.DesktopDeviceToken = endpoint;
                    await _db.SaveChangesAsync();
                }

                _logger.LogInformation("Created new web push subscription for user {UserId}", userId);
                return endpoint;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing user {UserId} to web push", userId);
                throw;
            }
        }

        public async Task<bool> UnsubscribeUserAsync(int userId, string endpoint)
        {
            try
            {
                var subscription = await _db.WebPushSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

                if (subscription != null)
                {
                    _db.WebPushSubscriptions.Remove(subscription);
                    await _db.SaveChangesAsync();
                    
                    _logger.LogInformation("Removed web push subscription for user {UserId}", userId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing user {UserId} from web push", userId);
                return false;
            }
        }

        public async Task<List<string>> GetUserDeviceTokensAsync(int userId)
        {
            try
            {
                var tokens = await _db.WebPushSubscriptions
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Endpoint)
                    .ToListAsync();

                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device tokens for user {UserId}", userId);
                return new List<string>();
            }
        }

        private async Task RemoveInvalidSubscriptionAsync(string endpoint)
        {
            try
            {
                var subscription = await _db.WebPushSubscriptions
                    .FirstOrDefaultAsync(s => s.Endpoint == endpoint);

                if (subscription != null)
                {
                    _db.WebPushSubscriptions.Remove(subscription);
                    await _db.SaveChangesAsync();
                    
                    _logger.LogInformation("Removed invalid web push subscription: {Endpoint}", endpoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing invalid subscription: {Endpoint}", endpoint);
            }
        }

        private static (string PublicKey, string PrivateKey) GenerateVapidKeys()
        {
            var vapidKeys = VapidHelper.GenerateVapidKeys();
            return (vapidKeys.PublicKey, vapidKeys.PrivateKey);
        }
    }
}
