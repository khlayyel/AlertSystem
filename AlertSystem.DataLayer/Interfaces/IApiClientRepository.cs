using AlertSystem.Entities.Entities;

namespace AlertSystem.DataLayer.Interfaces
{
    /// <summary>
    /// Repository interface for ApiClient entity operations
    /// </summary>
    public interface IApiClientRepository
    {
        Task<ApiClient?> GetByIdAsync(int id);
        Task<ApiClient?> GetByApiKeyHashAsync(string apiKeyHash);
        Task<IEnumerable<ApiClient>> GetAllAsync();
        Task<IEnumerable<ApiClient>> GetActiveClientsAsync();
        Task<ApiClient> CreateAsync(ApiClient apiClient);
        Task<ApiClient> UpdateAsync(ApiClient apiClient);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ApiKeyHashExistsAsync(string apiKeyHash);
        Task<int> CountAsync();
    }
}
