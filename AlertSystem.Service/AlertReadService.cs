using System;
using System.Linq;
using System.Threading.Tasks;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Service
{
    public sealed class AlertReadService : IAlertReadService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AlertReadService> _logger;

        public AlertReadService(ApplicationDbContext db, ILogger<AlertReadService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> GetUnreadCountAsync()
        {
            // 1 = Non Lu
            return await _db.HistoriqueAlertes
                .Where(d => d.EtatAlerteId == 1 || d.DateLecture == null)
                .CountAsync();
        }

        public async Task<int> GetTodayCountAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await _db.Alerte
                .Where(a => a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow)
                .CountAsync();
        }

        public async Task<int> GetConfirmedMandatoryCountAsync()
        {
            // Obligatoires confirmées = acquittement nécessaire ET lu/confirmé (EtatAlerteId = 2)
            return await _db.HistoriqueAlertes
                .Join(_db.Alerte, d => d.AlerteId, a => a.AlerteId, (d, a) => new { d, a })
                .Join(_db.AlertType, x => x.a.AlertTypeId, at => at.AlertTypeId, (x, at) => new { x.d, x.a, at })
                .Where(x => x.at.AlertTypeName == "acquittementNécessaire" && (x.d.EtatAlerteId == 2 || x.d.DateLecture != null))
                .CountAsync();
        }

        public async Task<int> GetMandatoryPendingCountAsync()
        {
            // Obligatoires en attente = acquittement nécessaire ET non lu (EtatAlerteId != 2 et DateLecture NULL)
            return await _db.HistoriqueAlertes
                .Join(_db.Alerte, d => d.AlerteId, a => a.AlerteId, (d, a) => new { d, a })
                .Join(_db.AlertType, x => x.a.AlertTypeId, at => at.AlertTypeId, (x, at) => new { x.d, x.a, at })
                .Where(x => x.at.AlertTypeName == "acquittementNécessaire" && (x.d.EtatAlerteId != 2 && x.d.DateLecture == null))
                .CountAsync();
        }

        public async Task<object> GetInboxAsync(int page, int size, string status = "all")
        {
            var query = _db.HistoriqueAlertes
                .Include(h => h.Alerte)
                .ThenInclude(a => a!.AlertType)
                .Include(h => h.Alerte)
                .ThenInclude(a => a!.Statut)
                .Include(h => h.Alerte)
                .ThenInclude(a => a!.Etat)
                .Include(h => h.Alerte)
                .ThenInclude(a => a!.Expediteur)
                .AsQueryable();

            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(h => h.Etat != null && h.Etat.EtatAlerteName == status);
            }

            var total = await query.CountAsync();
            var alerts = await query
                .OrderByDescending(h => h.Alerte!.DateCreationAlerte)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(h => new
                {
                    id = h.AlerteId,
                    historiqueId = h.HistoriqueAlerteId,
                    title = h.Alerte!.TitreAlerte,
                    message = h.Alerte.DescriptionAlerte,
                    type = h.Alerte.AlertType != null ? h.Alerte.AlertType.AlertTypeName : "Unknown",
                    status = h.Alerte.Statut != null ? h.Alerte.Statut.StatutName : "Unknown",
                    state = h.Etat != null ? h.Etat.EtatAlerteName : "Unknown",
                    etatAlerteId = h.EtatAlerteId,
                    createdAt = h.Alerte.DateCreationAlerte,
                    readAt = h.DateLecture,
                    senderName = h.Alerte.Expediteur != null ? h.Alerte.Expediteur.FullName : 
                                 (h.Alerte.ExpedTypeId == 1 ? "Inconnu" : "Système Automatique"),
                    sender = h.Alerte.Expediteur != null ? h.Alerte.Expediteur.FullName : 
                            (h.Alerte.ExpedTypeId == 1 ? "Inconnu" : "Système Automatique")
                })
                .ToListAsync();

            return new { items = alerts, total, page, size };
        }

        public async Task<object> GetHistoryAsync(string status, int page, int size)
        {
            var query = _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .AsQueryable();

            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Etat != null && a.Etat.EtatAlerteName == status);
            }

            var total = await query.CountAsync();
            var alerts = await query
                .OrderByDescending(a => a.DateCreationAlerte)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(a => new
                {
                    id = a.AlerteId,
                    title = a.TitreAlerte,
                    message = a.DescriptionAlerte,
                    type = a.AlertType != null ? a.AlertType.AlertTypeName : "Unknown",
                    status = a.Statut != null ? a.Statut.StatutName : "Unknown",
                    state = a.Etat != null ? a.Etat.EtatAlerteName : "Unknown",
                    createdAt = a.DateCreationAlerte,
                    readAt = a.HistoriqueAlertes.FirstOrDefault() != null ? a.HistoriqueAlertes.FirstOrDefault()!.DateLecture : null
                })
                .ToListAsync();

            return new { items = alerts, total, page, size };
        }

        public async Task<object> GetSentAsync(int page, int size)
        {
            var query = _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.HistoriqueAlertes)
                .AsQueryable();

            var total = await query.CountAsync();
            var alerts = await query
                .OrderByDescending(a => a.DateCreationAlerte)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(a => new
                {
                    id = a.AlerteId,
                    title = a.TitreAlerte,
                    message = a.DescriptionAlerte,
                    type = a.AlertType != null ? a.AlertType.AlertTypeName : "Unknown",
                    status = a.Statut != null ? a.Statut.StatutName : "Unknown",
                    createdAt = a.DateCreationAlerte,
                    recipientCount = a.HistoriqueAlertes.Count,
                    confirmedCount = a.HistoriqueAlertes.Count(d => d.EtatAlerteId == 2 || d.DateLecture != null)
                })
                .ToListAsync();

            return new { items = alerts, total, page, size };
        }

        public async Task<object?> GetDetailsAsync(int id)
        {
            var alert = await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.HistoriqueAlertes)
                .FirstOrDefaultAsync(a => a.AlerteId == id);

            if (alert == null) return null;

            return new
            {
                id = alert.AlerteId,
                title = alert.TitreAlerte,
                message = alert.DescriptionAlerte,
                type = alert.AlertType != null ? alert.AlertType.AlertTypeName : "Unknown",
                status = alert.Statut != null ? alert.Statut.StatutName : "Unknown",
                state = alert.Etat != null ? alert.Etat.EtatAlerteName : "Unknown",
                createdAt = alert.DateCreationAlerte,
                // sender removed from UI
                recipients = alert.HistoriqueAlertes
                    .Select(h => new { h.DestinataireId, h.DestinataireUserId, fullName = h.User != null ? h.User.FullName : string.Empty, email = h.DestinataireEmail, phone = h.DestinatairePhoneNumber, desktop = h.DestinataireDesktop, etatId = h.EtatAlerteId, luLe = h.DateLecture })
                    .ToList()
            };
        }

        public async Task<bool> MarkAsReadAsync(int alertRecipientId)
        {
            try
            {
                _logger.LogInformation("MarkAsReadAsync: Attempting to mark alert {AlertRecipientId} as read", alertRecipientId);
                
                // First try to find by HistoriqueAlerteId (if it's properly set)
                var historique = await _db.HistoriqueAlertes
                    .FirstOrDefaultAsync(h => h.HistoriqueAlerteId == alertRecipientId);

                // If not found, try to find by AlerteId (temporary workaround)
                if (historique == null)
                {
                    _logger.LogInformation("MarkAsReadAsync: Not found by HistoriqueAlerteId, trying by AlerteId {AlerteId}", alertRecipientId);
                    historique = await _db.HistoriqueAlertes
                        .FirstOrDefaultAsync(h => h.AlerteId == alertRecipientId);
                }

                if (historique == null) 
                {
                    _logger.LogWarning("MarkAsReadAsync: HistoriqueAlerte with AlerteId {AlertRecipientId} not found", alertRecipientId);
                    return false;
                }

                _logger.LogInformation("MarkAsReadAsync: Found HistoriqueAlerte with AlerteId {AlertRecipientId}, current EtatAlerteId: {EtatAlerteId}", 
                    alertRecipientId, historique.EtatAlerteId);

                // Mark as read (EtatAlerteId = 2) and set read date
                historique.EtatAlerteId = 2; // Lu
                historique.DateLecture = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                _logger.LogInformation("MarkAsReadAsync: Successfully marked HistoriqueAlerte with AlerteId {AlertRecipientId} as read", alertRecipientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkAsReadAsync: Error marking alert {AlertRecipientId} as read", alertRecipientId);
                return false;
            }
        }
    }
}


