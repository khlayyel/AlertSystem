using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AlertSystem.Data;
using AlertSystem.API.Middleware;
using AlertSystem.Entities.Entities;
using AlertSystem.Service;
using AlertSystem.Repository.Implementations;
using AlertSystem.DataLayer.Interfaces;
using Xunit;

namespace AlertSystem.Tests.Integration;

public class ApiKeyMiddlewareTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ApiKeyMiddleware _middleware;
    private readonly RequestDelegate _next;

    public ApiKeyMiddlewareTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"ApiKeyTest_{Guid.NewGuid()}"));
        
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<IApiClientRepository, ApiClientRepository>();
        services.AddScoped<IApiKeyValidator, ApiKeyValidator>();
        
        var provider = services.BuildServiceProvider();
        _db = provider.GetRequiredService<ApplicationDbContext>();
        
        var logger = provider.GetRequiredService<ILogger<ApiKeyMiddleware>>();
        
        _middleware = new ApiKeyMiddleware(context => Task.CompletedTask, logger);
        _next = context => Task.CompletedTask;
        
        SeedApiClients();
    }

    private void SeedApiClients()
    {
        _db.ApiClients.AddRange(new[]
        {
            new ApiClient
            {
                ApiClientId = 1,
                Name = "Test Client",
                ApiKeyHash = GetSha256Hash("test-key-123"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                RateLimitPerMinute = 100
            },
            new ApiClient
            {
                ApiClientId = 2,
                Name = "Inactive Client",
                ApiKeyHash = GetSha256Hash("inactive-key-456"),
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                RateLimitPerMinute = 50
            }
        });
        
        _db.SaveChanges();
    }

    private static string GetSha256Hash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return System.BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    [Fact]
    public async Task Middleware_ValidKey_SetsApiClientId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-KEY"] = "test-key-123";
        
        var serviceProvider = new ServiceCollection()
            .AddScoped<IApiKeyValidator>(_ => new ApiKeyValidator(new ApiClientRepository(_db)))
            .AddScoped<ApplicationDbContext>(_ => _db)
            .BuildServiceProvider();
        
        context.RequestServices = serviceProvider;

        // Act
        await _middleware.InvokeAsync(context, serviceProvider.GetRequiredService<IApiKeyValidator>(), _db);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(1, context.Items["ApiClientId"]);
        Assert.Equal("Test Client", context.Items["ApiClientName"]);
    }

    [Fact]
    public async Task Middleware_InvalidKey_Returns403()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-KEY"] = "invalid-key";
        
        var serviceProvider = new ServiceCollection()
            .AddScoped<IApiKeyValidator>(_ => new ApiKeyValidator(new ApiClientRepository(_db)))
            .AddScoped<ApplicationDbContext>(_ => _db)
            .BuildServiceProvider();
        
        context.RequestServices = serviceProvider;

        // Act
        await _middleware.InvokeAsync(context, serviceProvider.GetRequiredService<IApiKeyValidator>(), _db);

        // Assert
        Assert.Equal(403, context.Response.StatusCode);
        Assert.False(context.Items.ContainsKey("ApiClientId"));
    }

    [Fact]
    public async Task Middleware_InactiveKey_Returns403()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-KEY"] = "inactive-key-456";
        
        var serviceProvider = new ServiceCollection()
            .AddScoped<IApiKeyValidator>(_ => new ApiKeyValidator(new ApiClientRepository(_db)))
            .AddScoped<ApplicationDbContext>(_ => _db)
            .BuildServiceProvider();
        
        context.RequestServices = serviceProvider;

        // Act
        await _middleware.InvokeAsync(context, serviceProvider.GetRequiredService<IApiKeyValidator>(), _db);

        // Assert
        Assert.Equal(403, context.Response.StatusCode);
        Assert.False(context.Items.ContainsKey("ApiClientId"));
    }

    [Fact]
    public async Task Middleware_MissingKey_Returns401()
    {
        // Arrange
        var context = new DefaultHttpContext();
        // No X-API-KEY header
        
        var serviceProvider = new ServiceCollection()
            .AddScoped<IApiKeyValidator>(_ => new ApiKeyValidator(new ApiClientRepository(_db)))
            .AddScoped<ApplicationDbContext>(_ => _db)
            .BuildServiceProvider();
        
        context.RequestServices = serviceProvider;

        // Act
        await _middleware.InvokeAsync(context, serviceProvider.GetRequiredService<IApiKeyValidator>(), _db);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        Assert.False(context.Items.ContainsKey("ApiClientId"));
    }

    [Fact]
    public async Task Middleware_EmptyKey_Returns401()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-KEY"] = "";
        
        var serviceProvider = new ServiceCollection()
            .AddScoped<IApiKeyValidator>(_ => new ApiKeyValidator(new ApiClientRepository(_db)))
            .AddScoped<ApplicationDbContext>(_ => _db)
            .BuildServiceProvider();
        
        context.RequestServices = serviceProvider;

        // Act
        await _middleware.InvokeAsync(context, serviceProvider.GetRequiredService<IApiKeyValidator>(), _db);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        Assert.False(context.Items.ContainsKey("ApiClientId"));
    }

    [Fact]
    public async Task Middleware_CaseInsensitiveKey_Succeeds()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["x-api-key"] = "test-key-123"; // lowercase header name
        
        var serviceProvider = new ServiceCollection()
            .AddScoped<IApiKeyValidator>(_ => new ApiKeyValidator(new ApiClientRepository(_db)))
            .AddScoped<ApplicationDbContext>(_ => _db)
            .BuildServiceProvider();
        
        context.RequestServices = serviceProvider;

        // Act
        await _middleware.InvokeAsync(context, serviceProvider.GetRequiredService<IApiKeyValidator>(), _db);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(1, context.Items["ApiClientId"]);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
