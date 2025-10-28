using AlertSystem.Service.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Service.Services
{
    /// <summary>
    /// Service refactorisé pour la lecture d'alertes avec la nouvelle structure
    /// </summary>
    public sealed class AlertReadService : IAlertReadService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AlertReadService> _logger;

        public AlertReadService(ApplicationDbContext db, ILogger<AlertReadService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Compte le nombre d'alertes non lues (par enregistrement individuel)
        /// </summary>
        public async Task<int> GetUnreadCountAsync()
        {
            return await _db.Alerte
                .Where(a => a.EtatAlerteId == 1 || a.DateLecture == null)
                .CountAsync();
        }

        /// <summary>
        /// Compte le nombre d'alertes créées aujourd'hui
        /// </summary>
        public async Task<int> GetTodayCountAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await _db.Alerte
                .Where(a => a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow)
                .CountAsync();
        }

        /// <summary>
        /// Compte le nombre d'alertes obligatoires confirmées (par groupe)
        /// </summary>
        public async Task<int> GetConfirmedMandatoryCountAsync()
        {
            // Count distinct alert groups that are mandatory and have been confirmed
            return await _db.Alerte
                .Join(_db.AlertType, a => a.AlertTypeId, at => at.AlertTypeId, (a, at) => new { a, at })
                .Where(x => x.at.AlertTypeName == "acquittementNécessaire" && (x.a.EtatAlerteId == 2 || x.a.DateLecture != null))
                .Select(x => x.a.AlertGroupId)
                .Distinct()
                .CountAsync();
        }

        /// <summary>
        /// Compte le nombre d'alertes obligatoires non confirmées (par groupe)
        /// </summary>
        public async Task<int> GetUnconfirmedMandatoryCountAsync()
        {
            // Count distinct alert groups that are mandatory and have not been confirmed
            return await _db.Alerte
                .Join(_db.AlertType, a => a.AlertTypeId, at => at.AlertTypeId, (a, at) => new { a, at })
                .Where(x => x.at.AlertTypeName == "acquittementNécessaire" && x.a.EtatAlerteId == 1 && x.a.DateLecture == null)
                .Select(x => x.a.AlertGroupId)
                .Distinct()
                .CountAsync();
        }

        /// <summary>
        /// Compte le nombre d'alertes par statut
        /// </summary>
        public async Task<int> GetCountByStatusAsync(int statusId)
        {
            return await _db.Alerte
                .Where(a => a.StatutId == statusId)
                .CountAsync();
        }

        /// <summary>
        /// Compte le nombre d'alertes par type
        /// </summary>
        public async Task<int> GetCountByTypeAsync(int alertTypeId)
        {
            return await _db.Alerte
                .Where(a => a.AlertTypeId == alertTypeId)
                .CountAsync();
        }

        /// <summary>
        /// Compte le nombre d'alertes par plateforme
        /// </summary>
        public async Task<int> GetCountByPlatformAsync(int platformId)
        {
            return await _db.Alerte
                .Where(a => a.PlateformeEnvoieId == platformId)
                .CountAsync();
        }

        /// <summary>
        /// Récupère les alertes non lues avec pagination
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetUnreadAlertsAsync(int page = 1, int pageSize = 20)
        {
            return await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => a.EtatAlerteId == 1 || a.DateLecture == null)
                .OrderByDescending(a => a.DateCreationAlerte)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les alertes par groupe
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetAlertsByGroupAsync(Guid groupId)
        {
            return await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => a.AlertGroupId == groupId)
                .OrderBy(a => a.DateCreationAlerte)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les alertes par destinataire
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetAlertsByRecipientAsync(string recipient, int platformId)
        {
            var query = _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => a.PlateformeEnvoieId == platformId);

            return platformId switch
            {
                1 => await query.Where(a => a.DestinataireEmail == recipient).ToListAsync(), // Email
                2 => await query.Where(a => a.DestinatairePhoneNumber == recipient).ToListAsync(), // WhatsApp
                3 => await query.Where(a => a.DestinataireUserId.ToString() == recipient || a.DestinataireDesktop == recipient).ToListAsync(), // Desktop
                _ => new List<Alerte>()
            };
        }

        /// <summary>
        /// Récupère les alertes par période
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetAlertsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => a.DateCreationAlerte >= startDate && a.DateCreationAlerte <= endDate)
                .OrderByDescending(a => a.DateCreationAlerte)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les alertes en attente de traitement par le worker
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetPendingAlertsAsync()
        {
            return await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => (a.StatutId == 1 || a.StatutId == 4) && !a.ProcessedByWorker)
                .OrderBy(a => a.DateCreationAlerte)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les alertes par utilisateur
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetAlertsByUserAsync(int userId)
        {
            return await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => a.DestinataireUserId == userId)
                .OrderByDescending(a => a.DateCreationAlerte)
                .ToListAsync();
        }

        /// <summary>
        /// Marque une alerte comme lue
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int alertRecordId)
        {
            try
            {
                var alert = await _db.Alerte.FindAsync(alertRecordId);
                if (alert == null) return false;

                alert.EtatAlerteId = 2; // Lu
                alert.DateLecture = DateTime.UtcNow;
                
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking alert {AlertRecordId} as read", alertRecordId);
                return false;
            }
        }

        /// <summary>
        /// Marque toutes les alertes d'un groupe comme lues
        /// </summary>
        public async Task<bool> MarkGroupAsReadAsync(Guid groupId)
        {
            try
            {
                var alerts = await _db.Alerte
                    .Where(a => a.AlertGroupId == groupId)
                    .ToListAsync();

                foreach (var alert in alerts)
                {
                    alert.EtatAlerteId = 2; // Lu
                    alert.DateLecture = DateTime.UtcNow;
                }
                
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking alert group {GroupId} as read", groupId);
                return false;
            }
        }

        /// <summary>
        /// Récupère les statistiques d'alertes
        /// </summary>
        public async Task<AlertStatistics> GetStatisticsAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var totalAlerts = await _db.Alerte.CountAsync();
            var todayAlerts = await _db.Alerte
                .Where(a => a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow)
                .CountAsync();
            var unreadAlerts = await _db.Alerte
                .Where(a => a.EtatAlerteId == 1 || a.DateLecture == null)
                .CountAsync();
            var sentAlerts = await _db.Alerte
                .Where(a => a.StatutId == 2)
                .CountAsync();
            var failedAlerts = await _db.Alerte
                .Where(a => a.StatutId == 4)
                .CountAsync();

            return new AlertStatistics
            {
                TotalAlerts = totalAlerts,
                TodayAlerts = todayAlerts,
                UnreadAlerts = unreadAlerts,
                SentAlerts = sentAlerts,
                FailedAlerts = failedAlerts
            };
        }
    }

}
