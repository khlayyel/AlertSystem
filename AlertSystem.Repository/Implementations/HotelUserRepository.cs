using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Repository.Implementations
{
    /// <summary>
    /// Repository des utilisateurs (basé sur def_Utilisateur)
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
            return await _context.DefUtilisateur
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UtilisateurId == userId);
        }

        public async Task<IEnumerable<DefUtilisateur>> GetActiveUsersAsync()
        {
            // Pas de colonne 'active' pour le moment: retourner tous les utilisateurs
            return await _context.DefUtilisateur
                .AsNoTracking()
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<DefUtilisateur?> GetUserByEmailAsync(string email)
        {
            return await _context.DefUtilisateur
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
