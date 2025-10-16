using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;

namespace AlertSystem.Worker.Services
{
    public class WebPushNotifier : IWebPushNotifier
    {
        private readonly ILogger<WebPushNotifier> _logger;
        private readonly string _connectionString;
        private readonly string? _vapidPublicKey;
        private readonly string? _vapidPrivateKey;
        private readonly string? _vapidSubject;

        public WebPushNotifier(ILogger<WebPushNotifier> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found");
            _vapidPublicKey = configuration["WebPush:PublicKey"];
            _vapidPrivateKey = configuration["WebPush:PrivateKey"];
            _vapidSubject = configuration["WebPush:Subject"];
        }

        public async Task SendAsync(int userId, string title, string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_vapidPublicKey) || 
                string.IsNullOrWhiteSpace(_vapidPrivateKey) || 
                string.IsNullOrWhiteSpace(_vapidSubject))
            {
                _logger.LogWarning("WebPush VAPID configuration missing, skipping send");
                return;
            }

            try
            {
                var subscriptions = await GetUserSubscriptions(userId, cancellationToken);
                
                if (subscriptions.Count == 0)
                {
                    _logger.LogDebug("No WebPush subscriptions found for user {UserId}", userId);
                    return;
                }

                var client = new WebPushClient();
                var vapidDetails = new VapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);
                var payload = JsonSerializer.Serialize(new { title, body = message });

                foreach (var (endpoint, p256dh, auth) in subscriptions)
                {
                    try
                    {
                        var subscription = new PushSubscription(endpoint, p256dh, auth);
                        await client.SendNotificationAsync(subscription, payload, vapidDetails, cancellationToken);
                        _logger.LogInformation("WebPush notification sent to user {UserId}", userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send WebPush to endpoint for user {UserId}", userId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WebPush notifications for user {UserId}", userId);
            }
        }

        private async Task<List<(string endpoint, string p256dh, string auth)>> GetUserSubscriptions(int userId, CancellationToken cancellationToken)
        {
            var subscriptions = new List<(string, string, string)>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Endpoint, P256dh, Auth FROM dbo.WebPushSubscriptions WHERE UserId = @UserId";
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                subscriptions.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2)
                ));
            }

            return subscriptions;
        }
    }
}
