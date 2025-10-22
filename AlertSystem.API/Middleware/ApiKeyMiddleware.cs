using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AlertSystem.Service;
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

            // Hash the incoming API key using SHA256
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedKeyBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key!));
            var hashedApiKey = System.BitConverter.ToString(hashedKeyBytes).Replace("-", "").ToLower();

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
    }
}


