using AlertSystem.Entities.Entities;

namespace AlertSystem.DataLayer.Interfaces
{
    /// <summary>
    /// Repository interface for HistoriqueAlerte entity operations
    /// </summary>
    public interface IHistoriqueAlerteRepository
    {
        Task<IEnumerable<HistoriqueAlerte>> GetByAlerteIdAsync(int alerteId);
        Task<IEnumerable<HistoriqueAlerte>> GetByUserIdAsync(int userId);
        Task<HistoriqueAlerte?> GetByIdAsync(int destinataireId);
        Task<HistoriqueAlerte> CreateAsync(HistoriqueAlerte historique);
        Task<HistoriqueAlerte> UpdateAsync(HistoriqueAlerte historique);
        Task<bool> DeleteAsync(int destinataireId);
        Task<bool> ExistsAsync(int destinataireId);
        Task<int> CountByAlerteIdAsync(int alerteId);
        Task<IEnumerable<HistoriqueAlerte>> GetUnreadByUserIdAsync(int userId);
    }
}
