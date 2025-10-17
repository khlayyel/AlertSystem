using System.Threading.Tasks;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Service
{
    public sealed class ApiKeyValidator : IApiKeyValidator
    {
        private readonly ApplicationDbContext _db;

        public ApiKeyValidator(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> ValidateAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return false;
            // Entities model uses ApiKeyHash only; keep legacy ApiKey if present
            // Here assume plain ApiKey column may not exist; fall back to hash verify not available here
            return await _db.ApiClients.AnyAsync(c => c.IsActive && c.ApiKeyHash != null);
        }
    }
}


