using AlertSystem.Entities.Entities;

namespace AlertSystem.DataLayer.Interfaces
{
    /// <summary>
    /// Repository interface for Alerte entity operations
    /// </summary>
    public interface IAlerteRepository
    {
        Task<Alerte?> GetByIdAsync(int id);
        Task<IEnumerable<Alerte>> GetAllAsync();
        Task<IEnumerable<Alerte>> GetByStatusAsync(int statusId);
        Task<IEnumerable<Alerte>> GetByTypeAsync(int alertTypeId);
        Task<IEnumerable<Alerte>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Alerte> CreateAsync(Alerte alerte);
        Task<Alerte> UpdateAsync(Alerte alerte);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<int> CountAsync();
        Task<IEnumerable<Alerte>> GetPaginatedAsync(int page, int pageSize);
    }
}
