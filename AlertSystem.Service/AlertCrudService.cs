using System.Linq;
using System.Threading.Tasks;
using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Service
{
    public sealed class AlertCrudService : IAlertCrudService
    {
        private readonly ApplicationDbContext _db;
        public AlertCrudService(ApplicationDbContext db) { _db = db; }

        public async Task<object[]> GetQuickListAsync()
        {
            // Utiliser les alertes existantes comme "templates rapides" (les plus récentes)
            var items = await _db.Alerte
                .Include(a => a.AlertType)
                .OrderByDescending(a => a.DateCreationAlerte)
                .Take(20)
                .Select(a => new
                {
                    id = a.AlerteId,
                    title = a.TitreAlerte,
                    message = a.DescriptionAlerte,
                    type = a.AlertType != null ? a.AlertType.AlertTypeName : "Information"
                })
                .ToArrayAsync();

            return items;
        }

        public async Task<(bool Success, int AlertId, string? Error)> CreateFromTemplateAsync(string title, string message, string type)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
                return (false, 0, "Title and message are required");

            var alertTypeId = await _db.AlertType
                .Where(t => t.AlertTypeName == type)
                .Select(t => t.AlertTypeId)
                .FirstOrDefaultAsync();

            if (alertTypeId == 0)
            {
                alertTypeId = await _db.AlertType.Select(t => t.AlertTypeId).FirstAsync();
            }


            var statutId = await _db.Statut
                .Where(s => s.StatutName == "En Cours")
                .Select(s => s.StatutId)
                .FirstAsync();

            var etatId = await _db.Etat
                .Where(e => e.EtatAlerteName == "Non Lu")
                .Select(e => e.EtatAlerteId)
                .FirstAsync();

            var alert = new Alerte
            {
                TitreAlerte = title,
                DescriptionAlerte = message,
                AlertTypeId = alertTypeId,
                // ExpedType removed from UI scope; keep DB default
                StatutId = statutId,
                EtatAlerteId = etatId,
                DateCreationAlerte = System.DateTime.UtcNow,
                AppId = 1
            };

            _db.Alerte.Add(alert);
            await _db.SaveChangesAsync();
            return (true, alert.AlerteId, null);
        }
    }
}


