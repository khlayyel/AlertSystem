using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Repository.Implementations
{
    /// <summary>
    /// EF Core implementation of IHistoriqueAlerteRepository
    /// </summary>
    public class HistoriqueAlerteRepository : IHistoriqueAlerteRepository
    {
        private readonly IDbContext _context;

        public HistoriqueAlerteRepository(IDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<HistoriqueAlerte>> GetByAlerteIdAsync(int alerteId)
        {
            return await _context.HistoriqueAlertes
                .Include(h => h.Alerte)
                .Include(h => h.User)
                .Include(h => h.Etat)
                .Where(h => h.AlerteId == alerteId)
                .ToListAsync();
        }

        public async Task<IEnumerable<HistoriqueAlerte>> GetByUserIdAsync(int userId)
        {
            return await _context.HistoriqueAlertes
                .Include(h => h.Alerte)
                .Include(h => h.User)
                .Include(h => h.Etat)
                .Where(h => h.DestinataireUserId == userId)
                .ToListAsync();
        }

        public async Task<HistoriqueAlerte?> GetByIdAsync(int destinataireId)
        {
            return await _context.HistoriqueAlertes
                .Include(h => h.Alerte)
                .Include(h => h.User)
                .Include(h => h.Etat)
                .FirstOrDefaultAsync(h => h.DestinataireId == destinataireId);
        }

        public async Task<HistoriqueAlerte> CreateAsync(HistoriqueAlerte historique)
        {
            _context.HistoriqueAlertes.Add(historique);
            await _context.SaveChangesAsync();
            return historique;
        }

        public async Task<HistoriqueAlerte> UpdateAsync(HistoriqueAlerte historique)
        {
            _context.HistoriqueAlertes.Update(historique);
            await _context.SaveChangesAsync();
            return historique;
        }

        public async Task<bool> DeleteAsync(int destinataireId)
        {
            var historique = await _context.HistoriqueAlertes.FindAsync(destinataireId);
            if (historique == null) return false;

            _context.HistoriqueAlertes.Remove(historique);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int destinataireId)
        {
            return await _context.HistoriqueAlertes.AnyAsync(h => h.DestinataireId == destinataireId);
        }

        public async Task<int> CountByAlerteIdAsync(int alerteId)
        {
            return await _context.HistoriqueAlertes.CountAsync(h => h.AlerteId == alerteId);
        }

        public async Task<IEnumerable<HistoriqueAlerte>> GetUnreadByUserIdAsync(int userId)
        {
            return await _context.HistoriqueAlertes
                .Include(h => h.Alerte)
                .Include(h => h.User)
                .Include(h => h.Etat)
                .Where(h => h.DestinataireUserId == userId && h.EtatAlerteId == 1) // Non Lu
                .ToListAsync();
        }
    }
}
