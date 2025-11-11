using AlertSystem.Entities.Entities;

namespace AlertSystem.DataLayer.Interfaces
{
    /// <summary>
    /// Repository interface for RappelSuivant entity operations
    /// </summary>
    public interface IRappelSuivantRepository
    {
        Task<IEnumerable<RappelSuivant>> GetByAlerteIdAsync(long alerteId);
        Task<IEnumerable<RappelSuivant>> GetPendingRemindersAsync();
        Task<RappelSuivant?> GetByIdAsync(int rappelId);
        Task<RappelSuivant> CreateAsync(RappelSuivant rappel);
        Task<RappelSuivant> UpdateAsync(RappelSuivant rappel);
        Task<bool> DeleteAsync(int rappelId);
        Task<bool> ExistsAsync(int rappelId);
        Task<int> CountPendingByAlerteIdAsync(long alerteId);
        Task<IEnumerable<RappelSuivant>> GetOverdueRemindersAsync();
    }
}
