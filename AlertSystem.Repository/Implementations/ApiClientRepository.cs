using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Repository.Implementations
{
    /// <summary>
    /// EF Core implementation of IApiClientRepository
    /// </summary>
    public class ApiClientRepository : IApiClientRepository
    {
        private readonly IDbContext _context;

        public ApiClientRepository(IDbContext context)
        {
            _context = context;
        }

        public async Task<ApiClient?> GetByIdAsync(int id)
        {
            return await _context.ApiClients.FindAsync(id);
        }

        public async Task<ApiClient?> GetByApiKeyHashAsync(string apiKeyHash)
        {
            return await _context.ApiClients
                .FirstOrDefaultAsync(c => c.ApiKeyHash == apiKeyHash);
        }

        public async Task<IEnumerable<ApiClient>> GetAllAsync()
        {
            return await _context.ApiClients.ToListAsync();
        }

        public async Task<IEnumerable<ApiClient>> GetActiveClientsAsync()
        {
            return await _context.ApiClients
                .Where(c => c.IsActive)
                .ToListAsync();
        }

        public async Task<ApiClient> CreateAsync(ApiClient apiClient)
        {
            _context.ApiClients.Add(apiClient);
            await _context.SaveChangesAsync();
            return apiClient;
        }

        public async Task<ApiClient> UpdateAsync(ApiClient apiClient)
        {
            _context.ApiClients.Update(apiClient);
            await _context.SaveChangesAsync();
            return apiClient;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var apiClient = await _context.ApiClients.FindAsync(id);
            if (apiClient == null) return false;

            _context.ApiClients.Remove(apiClient);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.ApiClients.AnyAsync(c => c.ApiClientId == id);
        }

        public async Task<bool> ApiKeyHashExistsAsync(string apiKeyHash)
        {
            return await _context.ApiClients.AnyAsync(c => c.ApiKeyHash == apiKeyHash);
        }

        public async Task<int> CountAsync()
        {
            return await _context.ApiClients.CountAsync();
        }
    }
}
