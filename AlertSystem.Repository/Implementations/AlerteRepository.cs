using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Repository.Implementations
{
    /// <summary>
    /// EF Core implementation of IAlerteRepository
    /// </summary>
    public class AlerteRepository : IAlerteRepository
    {
        private readonly IDbContext _context;

        public AlerteRepository(IDbContext context)
        {
            _context = context;
        }

        public async Task<Alerte?> GetByIdAsync(int id)
        {
            return await _context.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .FirstOrDefaultAsync(a => a.AlertRecordId == id);
        }

        public async Task<IEnumerable<Alerte>> GetAllAsync()
        {
            return await _context.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alerte>> GetByStatusAsync(int statusId)
        {
            return await _context.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Where(a => a.StatutId == statusId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alerte>> GetByTypeAsync(int alertTypeId)
        {
            return await _context.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Where(a => a.AlertTypeId == alertTypeId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alerte>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Where(a => a.DateCreationAlerte >= startDate && a.DateCreationAlerte <= endDate)
                .ToListAsync();
        }

        public async Task<Alerte> CreateAsync(Alerte alerte)
        {
            _context.Alerte.Add(alerte);
            await _context.SaveChangesAsync();
            return alerte;
        }

        public async Task<Alerte> UpdateAsync(Alerte alerte)
        {
            _context.Alerte.Update(alerte);
            await _context.SaveChangesAsync();
            return alerte;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var alerte = await _context.Alerte.FindAsync(id);
            if (alerte == null) return false;

            _context.Alerte.Remove(alerte);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Alerte.AnyAsync(a => a.AlertRecordId == id);
        }

        public async Task<int> CountAsync()
        {
            return await _context.Alerte.CountAsync();
        }

        public async Task<IEnumerable<Alerte>> GetPaginatedAsync(int page, int pageSize)
        {
            return await _context.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère toutes les alertes d'un groupe spécifique
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetByGroupIdAsync(Guid groupId)
        {
            return await _context.Alerte
                .Include(a => a.AlertType)
                .Include(a => a.Statut)
                .Include(a => a.Etat)
                .Include(a => a.PlateformeEnvoie)
                .Include(a => a.DestinataireUser)
                .Where(a => a.AlertGroupId == groupId)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les alertes en attente de traitement par le worker
        /// </summary>
        public async Task<IEnumerable<Alerte>> GetPendingForWorkerAsync()
        {
            return await _context.Alerte
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
        /// Marque une alerte comme traitée par le worker
        /// </summary>
        public async Task MarkAsProcessedAsync(int alertRecordId)
        {
            var alerte = await _context.Alerte.FindAsync(alertRecordId);
            if (alerte != null)
            {
                alerte.ProcessedByWorker = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
