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
                await context.Response.WriteAsync("Missing API Key");
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing API Key");
                return;
            }

            // Hash the incoming API key using SHA256 (consistent with CryptoUtils)
            var hashedApiKey = AlertSystem.Utils.Crypto.CryptoUtils.ComputeSha256(key!);

            // Look up the API client by hashed key
            var apiClient = await db.ApiClients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IsActive && c.ApiKeyHash == hashedApiKey);

            if (apiClient == null)
            {
                _logger.LogWarning("Invalid API key from {RemoteIp}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Invalid API Key");
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


