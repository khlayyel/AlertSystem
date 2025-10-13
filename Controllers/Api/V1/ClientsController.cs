using Microsoft.AspNetCore.Mvc;
using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.Security.Cryptography;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public sealed class ClientsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(ApplicationDbContext db, ILogger<ClientsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { error = "Client name is required" });
                }

                // Generate a secure API key (32 bytes = 64 hex chars)
                var apiKeyBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(apiKeyBytes);
                }
                var apiKey = Convert.ToHexString(apiKeyBytes).ToLower();

                // Hash the API key using BCrypt
                var apiKeyHash = BCrypt.Net.BCrypt.HashPassword(apiKey, 12);

                var client = new ApiClient
                {
                    Name = dto.Name.Trim(),
                    ApiKeyHash = apiKeyHash,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    RateLimitPerMinute = dto.RateLimitPerMinute
                };

                _db.ApiClients.Add(client);
                await _db.SaveChangesAsync();

                _logger.LogInformation("API Client created: {ClientId} - {Name}", client.ApiClientId, client.Name);

                // Return the plain API key ONLY ONCE
                return CreatedAtAction(nameof(GetClient), new { id = client.ApiClientId }, new
                {
                    clientId = client.ApiClientId,
                    name = client.Name,
                    apiKey = apiKey, // Only returned once!
                    isActive = client.IsActive,
                    createdAt = client.CreatedAt,
                    rateLimitPerMinute = client.RateLimitPerMinute
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API client");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetClient(int id)
        {
            try
            {
                var client = await _db.ApiClients
                    .Where(c => c.ApiClientId == id)
                    .Select(c => new
                    {
                        clientId = c.ApiClientId,
                        name = c.Name,
                        isActive = c.IsActive,
                        createdAt = c.CreatedAt,
                        rateLimitPerMinute = c.RateLimitPerMinute
                        // Note: Never return the API key hash
                    })
                    .FirstOrDefaultAsync();

                if (client == null)
                {
                    return NotFound(new { error = "Client not found" });
                }

                return Ok(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API client {ClientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListClients(int page = 1, int size = 20)
        {
            try
            {
                var query = _db.ApiClients.AsQueryable();
                var total = await query.CountAsync();
                
                var clients = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(c => new
                    {
                        clientId = c.ApiClientId,
                        name = c.Name,
                        isActive = c.IsActive,
                        createdAt = c.CreatedAt,
                        rateLimitPerMinute = c.RateLimitPerMinute
                    })
                    .ToListAsync();

                return Ok(new { items = clients, total, page, size });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing API clients");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPatch("{id:int}/activate")]
        public async Task<IActionResult> ActivateClient(int id)
        {
            try
            {
                var client = await _db.ApiClients.FindAsync(id);
                if (client == null)
                {
                    return NotFound(new { error = "Client not found" });
                }

                client.IsActive = true;
                await _db.SaveChangesAsync();

                _logger.LogInformation("API Client activated: {ClientId} - {Name}", client.ApiClientId, client.Name);

                return Ok(new { message = "Client activated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating API client {ClientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> DeactivateClient(int id)
        {
            try
            {
                var client = await _db.ApiClients.FindAsync(id);
                if (client == null)
                {
                    return NotFound(new { error = "Client not found" });
                }

                client.IsActive = false;
                await _db.SaveChangesAsync();

                _logger.LogInformation("API Client deactivated: {ClientId} - {Name}", client.ApiClientId, client.Name);

                return Ok(new { message = "Client deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating API client {ClientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPatch("{id:int}/rate-limit")]
        public async Task<IActionResult> UpdateRateLimit(int id, [FromBody] UpdateRateLimitDto dto)
        {
            try
            {
                var client = await _db.ApiClients.FindAsync(id);
                if (client == null)
                {
                    return NotFound(new { error = "Client not found" });
                }

                client.RateLimitPerMinute = dto.RateLimitPerMinute;
                await _db.SaveChangesAsync();

                _logger.LogInformation("API Client rate limit updated: {ClientId} - {Name} - {RateLimit}/min", 
                    client.ApiClientId, client.Name, client.RateLimitPerMinute);

                return Ok(new { message = "Rate limit updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rate limit for API client {ClientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            try
            {
                var client = await _db.ApiClients.FindAsync(id);
                if (client == null)
                {
                    return NotFound(new { error = "Client not found" });
                }

                _db.ApiClients.Remove(client);
                await _db.SaveChangesAsync();

                _logger.LogWarning("API Client deleted: {ClientId} - {Name}", client.ApiClientId, client.Name);

                return Ok(new { message = "Client deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API client {ClientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        public sealed class CreateClientDto
        {
            public string Name { get; set; } = string.Empty;
            public int? RateLimitPerMinute { get; set; }
        }

        public sealed class UpdateRateLimitDto
        {
            public int? RateLimitPerMinute { get; set; }
        }
    }
}
