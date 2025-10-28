using AlertSystem.Entities.Entities;

namespace AlertSystem.DataLayer.Interfaces
{
    /// <summary>
    /// Repository pour accéder aux utilisateurs hotel (read-only)
    /// </summary>
    public interface IHotelUserRepository
    {
        Task<DefUtilisateur?> GetUserByIdAsync(int userId);
        Task<IEnumerable<DefUtilisateur>> GetActiveUsersAsync();
        Task<DefUtilisateur?> GetUserByEmailAsync(string email);
    }
}
