using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using AlertSystem.Services;

namespace AlertSystem.Middleware
{
    public sealed class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        public ApiKeyAuthenticationMiddleware(RequestDelegate next) { _next = next; }

        public async Task InvokeAsync(HttpContext context, IApiKeyValidator validator)
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // Allow creating API clients without authentication (bootstrap scenario)
                if (context.Request.Path.StartsWithSegments("/api/v1/clients") && 
                    context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                // Allow testing API keys without authentication (for validation)
                if (context.Request.Path.StartsWithSegments("/api/v1/keys/test") && 
                    context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                // Allow database seeding without authentication (bootstrap scenario)
                if (context.Request.Path.StartsWithSegments("/api/v1/seed"))
                {
                    await _next(context);
                    return;
                }

                // Allow test endpoints without authentication (debugging)
                if (context.Request.Path.StartsWithSegments("/api/v1/test") || 
                    context.Request.Path.StartsWithSegments("/api/v1/whatsapptest"))
                {
                    await _next(context);
                    return;
                }

                // All other API endpoints require valid API key
                string? apiKey = context.Request.Headers["X-Api-Key"].ToString();
                if (!await validator.IsValidAsync(apiKey))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "invalid_api_key" });
                    return;
                }
            }
            await _next(context);
        }
    }
}


