using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Repository.Implementations
{
    /// <summary>
    /// EF Core implementation of IRappelSuivantRepository
    /// </summary>
    public class RappelSuivantRepository : IRappelSuivantRepository
    {
        private readonly IDbContext _context;

        public RappelSuivantRepository(IDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RappelSuivant>> GetByAlerteIdAsync(int alerteId)
        {
            return await _context.RappelSuivant
                .Include(r => r.Alerte)
                .Include(r => r.Historique)
                .Where(r => r.AlerteId == alerteId)
                .ToListAsync();
        }

        public async Task<IEnumerable<RappelSuivant>> GetPendingRemindersAsync()
        {
            return await _context.RappelSuivant
                .Include(r => r.Alerte)
                .Include(r => r.Historique)
                .Where(r => r.DateRappel <= DateTime.UtcNow && r.StatutRappel == "En Attente")
                .ToListAsync();
        }

        public async Task<RappelSuivant?> GetByIdAsync(int rappelId)
        {
            return await _context.RappelSuivant
                .Include(r => r.Alerte)
                .Include(r => r.Historique)
                .FirstOrDefaultAsync(r => r.RappelId == rappelId);
        }

        public async Task<RappelSuivant> CreateAsync(RappelSuivant rappel)
        {
            _context.RappelSuivant.Add(rappel);
            await _context.SaveChangesAsync();
            return rappel;
        }

        public async Task<RappelSuivant> UpdateAsync(RappelSuivant rappel)
        {
            _context.RappelSuivant.Update(rappel);
            await _context.SaveChangesAsync();
            return rappel;
        }

        public async Task<bool> DeleteAsync(int rappelId)
        {
            var rappel = await _context.RappelSuivant.FindAsync(rappelId);
            if (rappel == null) return false;

            _context.RappelSuivant.Remove(rappel);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int rappelId)
        {
            return await _context.RappelSuivant.AnyAsync(r => r.RappelId == rappelId);
        }

        public async Task<int> CountPendingByAlerteIdAsync(int alerteId)
        {
            return await _context.RappelSuivant
                .CountAsync(r => r.AlerteId == alerteId && r.StatutRappel == "En Attente");
        }

        public async Task<IEnumerable<RappelSuivant>> GetOverdueRemindersAsync()
        {
            return await _context.RappelSuivant
                .Include(r => r.Alerte)
                .Include(r => r.Historique)
                .Where(r => r.DateRappel < DateTime.UtcNow.AddMinutes(-5) && r.StatutRappel == "En Attente")
                .ToListAsync();
        }
    }
}
