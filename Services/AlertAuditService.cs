using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AlertSystem.Services
{
    public sealed class AlertAuditService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AlertAuditService> _logger;

        public AlertAuditService(ApplicationDbContext db, ILogger<AlertAuditService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task LogAlertCreated(int alertId, int createdBy, string alertType, string title)
        {
            _logger.LogInformation("ALERT_CREATED: AlertId={AlertId}, CreatedBy={CreatedBy}, Type={Type}, Title={Title}", 
                alertId, createdBy, alertType, title);
        }

        public async Task LogAlertSent(int alertId, int recipientCount, List<string> platforms, TimeSpan cancellationWindow)
        {
            _logger.LogInformation("ALERT_SENT: AlertId={AlertId}, Recipients={Recipients}, Platforms={Platforms}, CancellationWindow={Window}s", 
                alertId, recipientCount, string.Join(",", platforms), cancellationWindow.TotalSeconds);
        }

        public async Task LogAlertCancelled(int alertId, int cancelledBy, TimeSpan timeBeforeCancellation)
        {
            _logger.LogWarning("ALERT_CANCELLED: AlertId={AlertId}, CancelledBy={CancelledBy}, TimeBeforeCancel={Time}s", 
                alertId, cancelledBy, timeBeforeCancellation.TotalSeconds);
        }

        public async Task LogReminderSent(int alertRecipientId, int reminderCount, List<string> platforms)
        {
            _logger.LogInformation("REMINDER_SENT: AlertRecipientId={Id}, ReminderCount={Count}, Platforms={Platforms}", 
                alertRecipientId, reminderCount, string.Join(",", platforms));
        }

        public async Task LogAlertConfirmed(int alertRecipientId, int userId, TimeSpan timeToConfirm)
        {
            _logger.LogInformation("ALERT_CONFIRMED: AlertRecipientId={Id}, UserId={UserId}, TimeToConfirm={Time}", 
                alertRecipientId, userId, timeToConfirm);
        }

        public async Task LogNotificationFailure(string platform, int userId, string error)
        {
            _logger.LogError("NOTIFICATION_FAILED: Platform={Platform}, UserId={UserId}, Error={Error}", 
                platform, userId, error);
        }

        public async Task<Dictionary<string, object>> GetAlertStatistics(DateTime from, DateTime to)
        {
            var stats = new Dictionary<string, object>();

            // Total alerts created
            var totalAlerts = await _db.Alerts
                .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
                .CountAsync();

            // Alerts by type
            var alertsByType = await _db.Alerts
                .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
                .GroupBy(a => a.AlertType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            // Confirmation rates
            var totalRecipients = await _db.AlertRecipients
                .Include(ar => ar.Alert)
                .Where(ar => ar.Alert.CreatedAt >= from && ar.Alert.CreatedAt <= to)
                .CountAsync();

            var confirmedRecipients = await _db.AlertRecipients
                .Include(ar => ar.Alert)
                .Where(ar => ar.Alert.CreatedAt >= from && ar.Alert.CreatedAt <= to && ar.IsConfirmed)
                .CountAsync();

            // Platform usage
            var platformStats = await _db.AlertRecipients
                .Include(ar => ar.Alert)
                .Where(ar => ar.Alert.CreatedAt >= from && ar.Alert.CreatedAt <= to)
                .Where(ar => !string.IsNullOrEmpty(ar.DeliveryPlatforms))
                .ToListAsync();

            var platformCounts = new Dictionary<string, int>();
            foreach (var recipient in platformStats)
            {
                try
                {
                    var platforms = JsonSerializer.Deserialize<List<string>>(recipient.DeliveryPlatforms) ?? new List<string>();
                    foreach (var platform in platforms)
                    {
                        platformCounts[platform] = platformCounts.GetValueOrDefault(platform, 0) + 1;
                    }
                }
                catch
                {
                    // Skip invalid JSON
                }
            }

            stats["TotalAlerts"] = totalAlerts;
            stats["AlertsByType"] = alertsByType;
            stats["TotalRecipients"] = totalRecipients;
            stats["ConfirmedRecipients"] = confirmedRecipients;
            stats["ConfirmationRate"] = totalRecipients > 0 ? (double)confirmedRecipients / totalRecipients * 100 : 0;
            stats["PlatformUsage"] = platformCounts;
            stats["Period"] = new { From = from, To = to };

            _logger.LogInformation("STATISTICS_GENERATED: Period={From} to {To}, TotalAlerts={Total}, ConfirmationRate={Rate}%", 
                from, to, totalAlerts, stats["ConfirmationRate"]);

            return stats;
        }
    }
}
