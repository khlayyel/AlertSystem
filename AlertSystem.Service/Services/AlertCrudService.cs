using System.Linq;
using System.Threading.Tasks;
using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Service.Services
{
    /// <summary>
    /// Service refactorisé pour les opérations CRUD d'alertes avec la nouvelle structure
    /// </summary>
    public sealed class AlertCrudService : IAlertCrudService
    {
        private readonly ApplicationDbContext _db;
        
        public AlertCrudService(ApplicationDbContext db) 
        { 
            _db = db; 
        }

        /// <summary>
        /// Récupère la liste rapide des alertes (templates)
        /// </summary>
        public async Task<object[]> GetQuickListAsync()
        {
            // Utiliser les alertes existantes comme "templates rapides" (les plus récentes)
            var items = await _db.Alerte
                .Include(a => a.AlertType)
                .OrderByDescending(a => a.DateCreationAlerte)
                .Take(20)
                .Select(a => new
                {
                    id = a.AlertRecordId,
                    title = a.TitreAlerte,
                    message = a.DescriptionAlerte,
                    type = a.AlertType != null ? a.AlertType.AlertTypeName : "acquittementNonNécessaire"
                })
                .ToArrayAsync();

            return items;
        }

        /// <summary>
        /// Crée une alerte à partir d'un template
        /// </summary>
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
                .FirstOrDefaultAsync();

            if (statutId == 0)
            {
                statutId = 1; // Default to "En Cours"
            }

            try
            {
                var alertGroupId = Guid.NewGuid();
                var now = DateTime.UtcNow;

                // Créer une alerte de base (sans destinataire spécifique)
                var alerte = new Alerte
                {
                    AlertGroupId = alertGroupId,
                    TitreAlerte = title,
                    DescriptionAlerte = message,
                    DateCreationAlerte = now,
                    AlertTypeId = alertTypeId,
                    StatutId = statutId,
                    EtatAlerteId = 1, // Non Lu
                    PlateformeEnvoieId = 1, // Email par défaut
                    ProcessedByWorker = false
                };

                _db.Alerte.Add(alerte);
                await _db.SaveChangesAsync();

                return (true, alerte.AlertRecordId, null);
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }

        /// <summary>
        /// Récupère une alerte par son ID
        /// </summary>
        public async Task<Alerte?> GetByIdAsync(int alertRecordId)
        {
            return await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .FirstOrDefaultAsync(a => a.AlertRecordId == alertRecordId);
        }

        /// <summary>
        /// Récupère toutes les alertes d'un groupe
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetByGroupIdAsync(Guid groupId)
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
        /// Met à jour une alerte
        /// </summary>
        public async Task<bool> UpdateAsync(Alerte alerte)
        {
            try
            {
                _db.Alerte.Update(alerte);
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Supprime une alerte
        /// </summary>
        public async Task<bool> DeleteAsync(int alertRecordId)
        {
            try
            {
                var alerte = await _db.Alerte.FindAsync(alertRecordId);
                if (alerte == null) return false;

                _db.Alerte.Remove(alerte);
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Supprime toutes les alertes d'un groupe
        /// </summary>
        public async Task<bool> DeleteGroupAsync(Guid groupId)
        {
            try
            {
                var alertes = await _db.Alerte
                    .Where(a => a.AlertGroupId == groupId)
                    .ToListAsync();

                if (!alertes.Any()) return false;

                _db.Alerte.RemoveRange(alertes);
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Récupère les alertes avec pagination
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetPaginatedAsync(int page = 1, int pageSize = 20)
        {
            return await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .OrderByDescending(a => a.DateCreationAlerte)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les alertes par statut
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetByStatusAsync(int statusId)
        {
            return await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => a.StatutId == statusId)
                .OrderByDescending(a => a.DateCreationAlerte)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les alertes par type
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetByTypeAsync(int alertTypeId)
        {
            return await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => a.AlertTypeId == alertTypeId)
                .OrderByDescending(a => a.DateCreationAlerte)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les alertes par plateforme
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetByPlatformAsync(int platformId)
        {
            return await _db.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => a.PlateformeEnvoieId == platformId)
                .OrderByDescending(a => a.DateCreationAlerte)
                .ToListAsync();
        }

        /// <summary>
        /// Compte le nombre total d'alertes
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            return await _db.Alerte.CountAsync();
        }

        /// <summary>
        /// Compte le nombre d'alertes par statut
        /// </summary>
        public async Task<int> GetCountByStatusAsync(int statusId)
        {
            return await _db.Alerte.CountAsync(a => a.StatutId == statusId);
        }

        /// <summary>
        /// Compte le nombre d'alertes par type
        /// </summary>
        public async Task<int> GetCountByTypeAsync(int alertTypeId)
        {
            return await _db.Alerte.CountAsync(a => a.AlertTypeId == alertTypeId);
        }

        /// <summary>
        /// Compte le nombre d'alertes par plateforme
        /// </summary>
        public async Task<int> GetCountByPlatformAsync(int platformId)
        {
            return await _db.Alerte.CountAsync(a => a.PlateformeEnvoieId == platformId);
        }
    }
}
