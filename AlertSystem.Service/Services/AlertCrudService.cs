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

        public async Task<object[]> GetQuickListAsync()
        {
            var items = await _db.Alerte
                .OrderByDescending(a => a.DateCreationAlerte)
                .Take(20)
                .Select(a => new
                {
                    id = a.AlertRecordId,
                    title = a.TitreAlerte,
                    message = a.DescriptionAlerte,
                    type = a.TypeEnvoieId == 2 ? "acquittementNecessaire" : "acquittementNonNecessaire"
                })
                .ToArrayAsync();

            return items;
        }

        public async Task<(bool Success, int AlertId, string? Error)> CreateFromTemplateAsync(string title, string message, string type)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
                return (false, 0, "Title and message are required");

            var typeEnvoieId = type == "acquittementNecessaire" ? 2 : 1;

            var statutId = await _db.Statut
                .Where(s => s.Description == "EnCours")
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

                var alerte = new Alerte
                {
                    AlertGroupId = alertGroupId,
                    TitreAlerte = title,
                    DescriptionAlerte = message,
                    DateCreationAlerte = now,
                    TypeEnvoieId = typeEnvoieId,
                    StatutId = statutId,
                    EtatId = 1, // Non Lu
                    PlateformeEnvoieId = 1, // Email par défaut
                    ProcessedByWorker = false,
                    Destinataire = string.Empty,
                    AppId = 1
                };

                _db.Alerte.Add(alerte);
                await _db.SaveChangesAsync();

                return (true, (int)alerte.AlertRecordId, null);
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }

        public async Task<Alerte?> GetByIdAsync(int alertRecordId)
        {
            return await _db.Alerte
                .Include(a => a.TypeEnvoie)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .FirstOrDefaultAsync(a => a.AlertRecordId == alertRecordId);
        }

        public async Task<IEnumerable<Alerte>> GetByGroupIdAsync(Guid groupId)
        {
            return await _db.Alerte
                .Include(a => a.TypeEnvoie)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Where(a => a.AlertGroupId == groupId)
                .OrderBy(a => a.DateCreationAlerte)
                .ToListAsync();
        }

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

        public async Task<IEnumerable<Alerte>> GetPaginatedAsync(int page = 1, int pageSize = 20)
        {
            return await _db.Alerte
                .Include(a => a.TypeEnvoie)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .OrderByDescending(a => a.DateCreationAlerte)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alerte>> GetByStatusAsync(int statusId)
        {
            return await _db.Alerte
                .Include(a => a.TypeEnvoie)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Where(a => a.StatutId == statusId)
                .OrderByDescending(a => a.DateCreationAlerte)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alerte>> GetByTypeAsync(int typeEnvoieId)
        {
            return await _db.Alerte
                .Include(a => a.TypeEnvoie)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Where(a => a.TypeEnvoieId == typeEnvoieId)
                .OrderByDescending(a => a.DateCreationAlerte)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alerte>> GetByPlatformAsync(int platformId)
        {
            return await _db.Alerte
                .Include(a => a.TypeEnvoie)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Where(a => a.PlateformeEnvoieId == platformId)
                .OrderByDescending(a => a.DateCreationAlerte)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _db.Alerte.CountAsync();
        }

        public async Task<int> GetCountByStatusAsync(int statusId)
        {
            return await _db.Alerte.CountAsync(a => a.StatutId == statusId);
        }

        public async Task<int> GetCountByTypeAsync(int typeEnvoieId)
        {
            return await _db.Alerte.CountAsync(a => a.TypeEnvoieId == typeEnvoieId);
        }

        public async Task<int> GetCountByPlatformAsync(int platformId)
        {
            return await _db.Alerte.CountAsync(a => a.PlateformeEnvoieId == platformId);
        }
    }
}
