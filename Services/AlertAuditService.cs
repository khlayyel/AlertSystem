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

        public Task LogAlertCreated(int alertId, int createdBy, string alertType, string title)
        {
            _logger.LogInformation("ALERT_CREATED: AlertId={AlertId}, CreatedBy={CreatedBy}, Type={Type}, Title={Title}", 
                alertId, createdBy, alertType, title);
            return Task.CompletedTask;
        }

        public Task LogAlertSent(int alertId, int recipientCount, List<string> platforms, TimeSpan cancellationWindow)
        {
            _logger.LogInformation("ALERT_SENT: AlertId={AlertId}, Recipients={Recipients}, Platforms={Platforms}, CancellationWindow={Window}s", 
                alertId, recipientCount, string.Join(",", platforms), cancellationWindow.TotalSeconds);
            return Task.CompletedTask;
        }

        public Task LogAlertCancelled(int alertId, int cancelledBy, TimeSpan timeBeforeCancellation)
        {
            _logger.LogWarning("ALERT_CANCELLED: AlertId={AlertId}, CancelledBy={CancelledBy}, TimeBeforeCancel={Time}s", 
                alertId, cancelledBy, timeBeforeCancellation.TotalSeconds);
            return Task.CompletedTask;
        }

        public Task LogReminderSent(int alertRecipientId, int reminderCount, List<string> platforms)
        {
            _logger.LogInformation("REMINDER_SENT: AlertRecipientId={Id}, ReminderCount={Count}, Platforms={Platforms}", 
                alertRecipientId, reminderCount, string.Join(",", platforms));
            return Task.CompletedTask;
        }

        public Task LogAlertConfirmed(int alertRecipientId, int userId, TimeSpan timeToConfirm)
        {
            _logger.LogInformation("ALERT_CONFIRMED: AlertRecipientId={Id}, UserId={UserId}, TimeToConfirm={Time}", 
                alertRecipientId, userId, timeToConfirm);
            return Task.CompletedTask;
        }

        public Task LogNotificationFailure(string platform, int userId, string error)
        {
            _logger.LogError("NOTIFICATION_FAILED: Platform={Platform}, UserId={UserId}, Error={Error}", 
                platform, userId, error);
            return Task.CompletedTask;
        }

        public async Task<Dictionary<string, object>> GetAlertStatistics(DateTime from, DateTime to)
        {
            var stats = new Dictionary<string, object>();

            // Total alerts created (nouveau modèle)
            var totalAlerts = await _db.Alerte
                .Where(a => a.DateCreationAlerte >= from && a.DateCreationAlerte <= to)
                .CountAsync();

            // Alerts by type (via table de référence)
            var alertsByType = await _db.Alerte
                .Where(a => a.DateCreationAlerte >= from && a.DateCreationAlerte <= to)
                .GroupBy(a => a.AlertTypeId)
                .Select(g => new { AlertTypeId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Confirmation rates (destinataires lus/non lus)
            var totalRecipients = await _db.Destinataire
                .Join(_db.Alerte, d => d.AlerteId, a => a.AlerteId, (d, a) => new { d, a })
                .Where(x => x.a.DateCreationAlerte >= from && x.a.DateCreationAlerte <= to)
                .CountAsync();

            var confirmedRecipients = await _db.Destinataire
                .Join(_db.Alerte, d => d.AlerteId, a => a.AlerteId, (d, a) => new { d, a })
                .Where(x => x.a.DateCreationAlerte >= from && x.a.DateCreationAlerte <= to)
                .Where(x => x.d.EtatAlerte == "Lu" || x.d.DateLecture != null)
                .CountAsync();

            stats["TotalAlerts"] = totalAlerts;
            stats["AlertsByType"] = alertsByType;
            stats["TotalRecipients"] = totalRecipients;
            stats["ConfirmedRecipients"] = confirmedRecipients;
            stats["ConfirmationRate"] = totalRecipients > 0 ? (double)confirmedRecipients / totalRecipients * 100 : 0;
            stats["Period"] = new { From = from, To = to };

            _logger.LogInformation("STATISTICS_GENERATED: Period={From} to {To}, TotalAlerts={Total}, ConfirmationRate={Rate}%", 
                from, to, totalAlerts, stats["ConfirmationRate"]);

            return stats;
        }
    }
}
