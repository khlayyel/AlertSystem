using System;
using System.Linq;
using System.Threading.Tasks;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Service
{
    public sealed class AlertReadService : IAlertReadService
    {
        private readonly ApplicationDbContext _db;

        public AlertReadService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<int> GetUnreadCountAsync()
        {
            return await _db.HistoriqueAlertes
                .Where(d => d.EtatAlerte == "Non Lu" || d.DateLecture == null)
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
            // Obligatoires confirmées = acquittement nécessaire ET lu/confirmé
            return await _db.HistoriqueAlertes
                .Join(_db.Alerte, d => d.AlerteId, a => a.AlerteId, (d, a) => new { d, a })
                .Join(_db.AlertType, x => x.a.AlertTypeId, at => at.AlertTypeId, (x, at) => new { x.d, x.a, at })
                .Where(x => x.at.AlertTypeName == "acquittementNécessaire" && (x.d.EtatAlerte == "Lu" || x.d.DateLecture != null))
                .CountAsync();
        }

        public async Task<int> GetMandatoryPendingCountAsync()
        {
            // Obligatoires en attente = acquittement nécessaire ET non lu/non confirmé
            return await _db.HistoriqueAlertes
                .Join(_db.Alerte, d => d.AlerteId, a => a.AlerteId, (d, a) => new { d, a })
                .Join(_db.AlertType, x => x.a.AlertTypeId, at => at.AlertTypeId, (x, at) => new { x.d, x.a, at })
                .Where(x => x.at.AlertTypeName == "acquittementNécessaire" && (x.d.EtatAlerte != "Lu" && x.d.DateLecture == null))
                .CountAsync();
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
                    confirmedCount = a.HistoriqueAlertes.Count(d => d.EtatAlerte == "Lu" || d.DateLecture != null)
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
                    .Select(h => new { h.DestinataireId, h.DestinataireUserId, fullName = h.User != null ? h.User.FullName : string.Empty, email = h.DestinataireEmail, phone = h.DestinatairePhoneNumber, desktop = h.DestinataireDesktop, etat = h.EtatAlerte, luLe = h.DateLecture })
                    .ToList()
            };
        }
    }
}


