using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Repository.Implementations
{
    /// <summary>
    /// Implémentation du repository pour les utilisateurs hotel (read-only)
    /// </summary>
    public sealed class HotelUserRepository : IHotelUserRepository
    {
        private readonly IDbContext _context;

        public HotelUserRepository(IDbContext context)
        {
            _context = context;
        }

        public async Task<DefUtilisateur?> GetUserByIdAsync(int userId)
        {
            return await _context.DefUtilisateurs
                .FirstOrDefaultAsync(u => u.util_id == userId && u.util_compte_active);
        }

        public async Task<IEnumerable<DefUtilisateur>> GetActiveUsersAsync()
        {
            return await _context.DefUtilisateurs
                .Where(u => u.util_compte_active)
                .OrderBy(u => u.util_nom)
                .ToListAsync();
        }

        public async Task<DefUtilisateur?> GetUserByEmailAsync(string email)
        {
            return await _context.DefUtilisateurs
                .FirstOrDefaultAsync(u => u.util_email == email && u.util_compte_active);
        }
    }
}
