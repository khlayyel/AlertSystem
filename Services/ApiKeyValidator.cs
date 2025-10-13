using System.Threading.Tasks;
using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AlertSystem.Services
{
    public sealed class ApiKeyValidator : IApiKeyValidator
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ApiKeyValidator> _logger;

        public ApiKeyValidator(ApplicationDbContext db, IMemoryCache cache, ILogger<ApiKeyValidator> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> IsValidAsync(string? apiKey)
        {
            return await ValidateApiKeyAsync(apiKey);
        }

        public async Task<bool> ValidateApiKeyAsync(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return false;

            var client = await GetClientByApiKeyAsync(apiKey);
            if (client == null) return false;

            // Check rate limiting if configured
            if (client.RateLimitPerMinute.HasValue)
            {
                var rateLimitKey = $"rate_limit_{client.ApiClientId}_{DateTime.UtcNow:yyyy-MM-dd-HH-mm}";
                var currentCount = _cache.Get<int>(rateLimitKey);
                
                if (currentCount >= client.RateLimitPerMinute.Value)
                {
                    _logger.LogWarning("Rate limit exceeded for API client {ClientId} ({Name}): {Count}/{Limit} requests per minute", 
                        client.ApiClientId, client.Name, currentCount, client.RateLimitPerMinute.Value);
                    return false;
                }

                // Increment rate limit counter
                _cache.Set(rateLimitKey, currentCount + 1, TimeSpan.FromMinutes(1));
            }

            return true;
        }

        public async Task<ApiClient?> GetClientByApiKeyAsync(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            try
            {
                // Cache the client lookup for a short time to improve performance
                var cacheKey = $"api_client_lookup_{apiKey.GetHashCode()}";
                if (_cache.TryGetValue(cacheKey, out ApiClient? cachedClient))
                {
                    return cachedClient;
                }

                // Get all active clients and verify the API key hash
                var clients = await _db.ApiClients.AsNoTracking().Where(c => c.IsActive).ToListAsync();
                
                foreach (var client in clients)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(client.ApiKeyHash) && 
                            BCrypt.Net.BCrypt.Verify(apiKey, client.ApiKeyHash))
                        {
                            // Cache the result for 5 minutes
                            _cache.Set(cacheKey, client, TimeSpan.FromMinutes(5));
                            return client;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error verifying API key hash for client {ClientId}", client.ApiClientId);
                    }
                }

                // Cache negative result for 1 minute to prevent brute force
                _cache.Set(cacheKey, (ApiClient?)null, TimeSpan.FromMinutes(1));
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up API client");
                return null;
            }
        }
    }
}


