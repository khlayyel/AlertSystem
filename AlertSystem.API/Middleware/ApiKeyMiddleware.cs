using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AlertSystem.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;

namespace AlertSystem.API.Middleware
{
    public sealed class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;

        public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IApiKeyValidator validator, ApplicationDbContext db)
        {
            // Check if this endpoint is exempt from API key validation
            if (IsExemptEndpoint(context))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var key))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing API key");
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing API key");
                return;
            }

            // Hash the incoming API key using SHA256 (consistent with CryptoUtils)
            var hashedApiKey = AlertSystem.Utils.Crypto.CryptoUtils.ComputeSha256(key!);

            // Look up the API client by hashed key (project only needed fields)
            var apiClient = await db.ApiClients
                .AsNoTracking()
                .Where(c => c.ApiKeyHash == hashedApiKey)
                .Select(c => new {
                    c.ApiClientId,
                    c.Name,
                    c.IsActive,
                    ExpiresAt = EF.Property<DateTime?>(c, "ExpiresAt")
                })
                .FirstOrDefaultAsync();

            if (apiClient == null)
            {
                _logger.LogWarning("Invalid API key from {RemoteIp}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API key");
                return;
            }

            if (!apiClient.IsActive)
            {
                _logger.LogWarning("Inactive API key used by {RemoteIp}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("API key inactive");
                return;
            }

            if (apiClient.ExpiresAt.HasValue && apiClient.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired API key used by {RemoteIp}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("API key expired");
                return;
            }

            // Set the ApiClientId in HttpContext.Items for use in controllers
            context.Items["ApiClientId"] = apiClient.ApiClientId;
            context.Items["ApiClientName"] = apiClient.Name;

            await _next(context);
        }

        private static bool IsExemptEndpoint(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();
            var method = context.Request.Method.ToUpperInvariant();

            // Endpoints exemptés de l'authentification par clé API
            var exemptEndpoints = new[]
            {
                // Tests de connectivité
                "/api/v1/test/ping",
                "/api/v1/test/health",
                "/api/v1/test/database",
                // Stock alerts JSON feed for local worker polling
                "/api/v1/stock-alerts",

                // Public confirmation page (email/WhatsApp links)
                "/confirm",
                // Mock data endpoints used by local workers
                "/api/v1/stock",
                "/mock/stock",
                // Initialisation de la base de données
                "/api/v1/seed/database",
                "/api/v1/seed/status",
                
                // Création de nouveaux clients API (chicken-and-egg problem)
                "/api/v1/clients", // POST seulement
                
                // Test de clé API
                "/api/v1/keys/test",
                
                // Documentation Swagger
                "/swagger",
                "/swagger/",
                "/swagger/v1/swagger.json",
                
                // Fichiers statiques
                "/favicon.ico",
                "/robots.txt"
            };

            // Vérifier les endpoints exacts
            if (exemptEndpoints.Contains(path))
            {
                return true;
            }

            // Vérifier les endpoints avec méthodes spécifiques
            if (path == "/api/v1/clients" && method == "POST")
            {
                return true;
            }

            // Vérifier les préfixes Swagger
            if (path?.StartsWith("/swagger") == true)
            {
                return true;
            }

            return false;
        }
    }
}


