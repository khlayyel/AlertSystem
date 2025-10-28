using Microsoft.Extensions.Logging;
using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

namespace AlertSystem.Service.Services
{
    /// <summary>
    /// Service refactorisé pour l'audit des alertes avec la nouvelle structure
    /// </summary>
    public sealed class AlertAuditService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AlertAuditService> _logger;

        public AlertAuditService(ApplicationDbContext db, ILogger<AlertAuditService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Enregistre la création d'une alerte
        /// </summary>
        public Task LogAlertCreated(int alertRecordId, int createdBy, string alertType, string title)
        {
            _logger.LogInformation("ALERT_CREATED: AlertRecordId={AlertRecordId}, CreatedBy={CreatedBy}, Type={Type}, Title={Title}", 
                alertRecordId, createdBy, alertType, title);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enregistre l'envoi d'une alerte
        /// </summary>
        public Task LogAlertSent(int alertRecordId, int recipientCount, List<string> platforms, TimeSpan cancellationWindow)
        {
            _logger.LogInformation("ALERT_SENT: AlertRecordId={AlertRecordId}, Recipients={Recipients}, Platforms={Platforms}, CancellationWindow={Window}s", 
                alertRecordId, recipientCount, string.Join(",", platforms), cancellationWindow.TotalSeconds);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enregistre l'annulation d'une alerte
        /// </summary>
        public Task LogAlertCancelled(int alertRecordId, int cancelledBy, TimeSpan timeBeforeCancellation)
        {
            _logger.LogWarning("ALERT_CANCELLED: AlertRecordId={AlertRecordId}, CancelledBy={CancelledBy}, TimeBeforeCancel={Time}s", 
                alertRecordId, cancelledBy, timeBeforeCancellation.TotalSeconds);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enregistre l'envoi d'un rappel
        /// </summary>
        public Task LogReminderSent(int alertRecordId, int reminderCount, List<string> platforms)
        {
            _logger.LogInformation("REMINDER_SENT: AlertRecordId={Id}, ReminderCount={Count}, Platforms={Platforms}", 
                alertRecordId, reminderCount, string.Join(",", platforms));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enregistre la confirmation d'une alerte
        /// </summary>
        public Task LogAlertConfirmed(int alertRecordId, int userId, TimeSpan timeToConfirm)
        {
            _logger.LogInformation("ALERT_CONFIRMED: AlertRecordId={Id}, UserId={UserId}, TimeToConfirm={Time}s", 
                alertRecordId, userId, timeToConfirm.TotalSeconds);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enregistre l'échec d'une alerte
        /// </summary>
        public Task LogAlertFailed(int alertRecordId, string error, string platform)
        {
            _logger.LogError("ALERT_FAILED: AlertRecordId={Id}, Error={Error}, Platform={Platform}", 
                alertRecordId, error, platform);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enregistre la lecture d'une alerte
        /// </summary>
        public Task LogAlertRead(int alertRecordId, int userId, TimeSpan timeToRead)
        {
            _logger.LogInformation("ALERT_READ: AlertRecordId={Id}, UserId={UserId}, TimeToRead={Time}s", 
                alertRecordId, userId, timeToRead.TotalSeconds);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Récupère l'historique d'audit d'une alerte
        /// </summary>
        public async Task<IEnumerable<AlertAuditEntry>> GetAlertAuditHistoryAsync(int alertRecordId)
        {
            // Pour l'instant, on retourne les logs de l'application
            // Dans une implémentation complète, on pourrait avoir une table d'audit dédiée
            var alert = await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .FirstOrDefaultAsync(a => a.AlertRecordId == alertRecordId);

            if (alert == null)
                return new List<AlertAuditEntry>();

            var history = new List<AlertAuditEntry>
            {
                new AlertAuditEntry
                {
                    Timestamp = alert.DateCreationAlerte,
                    Action = "CREATED",
                    Details = $"Alert created: {alert.TitreAlerte}",
                    UserId = (int?)alert.ExpediteurId,
                    Platform = alert.PlateformeEnvoie?.Plateforme ?? "Unknown"
                }
            };

            if (alert.DateLecture.HasValue)
            {
                history.Add(new AlertAuditEntry
                {
                    Timestamp = alert.DateLecture.Value,
                    Action = "READ",
                    Details = $"Alert read by {alert.DestinataireUser?.util_nom ?? "Unknown"}",
                    UserId = (int?)alert.DestinataireUserId,
                    Platform = alert.PlateformeEnvoie?.Plateforme ?? "Unknown"
                });
            }

            return history.OrderByDescending(h => h.Timestamp);
        }

        /// <summary>
        /// Récupère l'historique d'audit d'un groupe d'alertes
        /// </summary>
        public async Task<IEnumerable<AlertAuditEntry>> GetGroupAuditHistoryAsync(Guid groupId)
        {
            var alerts = await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => a.AlertGroupId == groupId)
                .OrderBy(a => a.DateCreationAlerte)
                .ToListAsync();

            var history = new List<AlertAuditEntry>();

            foreach (var alert in alerts)
            {
                history.Add(new AlertAuditEntry
                {
                    Timestamp = alert.DateCreationAlerte,
                    Action = "CREATED",
                    Details = $"Alert created: {alert.TitreAlerte}",
                    UserId = (int?)alert.ExpediteurId,
                    Platform = alert.PlateformeEnvoie?.Plateforme ?? "Unknown"
                });

                if (alert.DateLecture.HasValue)
                {
                    history.Add(new AlertAuditEntry
                    {
                        Timestamp = alert.DateLecture.Value,
                        Action = "READ",
                        Details = $"Alert read by {alert.DestinataireUser?.util_nom ?? "Unknown"}",
                        UserId = (int?)alert.DestinataireUserId,
                        Platform = alert.PlateformeEnvoie?.Plateforme ?? "Unknown"
                    });
                }
            }

            return history.OrderByDescending(h => h.Timestamp);
        }

        /// <summary>
        /// Récupère les statistiques d'audit
        /// </summary>
        public async Task<AuditStatistics> GetAuditStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _db.Alerte.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(a => a.DateCreationAlerte >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.DateCreationAlerte <= toDate.Value);

            var totalAlerts = await query.CountAsync();
            var sentAlerts = await query.CountAsync(a => a.StatutId == 2);
            var failedAlerts = await query.CountAsync(a => a.StatutId == 4);
            var readAlerts = await query.CountAsync(a => a.DateLecture.HasValue);
            var unreadAlerts = await query.CountAsync(a => !a.DateLecture.HasValue);

            return new AuditStatistics
            {
                TotalAlerts = totalAlerts,
                SentAlerts = sentAlerts,
                FailedAlerts = failedAlerts,
                ReadAlerts = readAlerts,
                UnreadAlerts = unreadAlerts
            };
        }
    }

    /// <summary>
    /// Entrée d'audit pour une alerte
    /// </summary>
    public class AlertAuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string Platform { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statistiques d'audit
    /// </summary>
    public class AuditStatistics
    {
        public int TotalAlerts { get; set; }
        public int SentAlerts { get; set; }
        public int FailedAlerts { get; set; }
        public int ReadAlerts { get; set; }
        public int UnreadAlerts { get; set; }
    }
}
