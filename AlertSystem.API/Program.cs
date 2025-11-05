using Microsoft.EntityFrameworkCore;
using AlertSystem.Service.Interfaces;
using AlertSystem.Service.Services;
using AlertSystem.API.Middleware;
using AlertSystem.Services;
using Serilog;
using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Repository.Implementations;
using AlertSystem.Utils.Configuration;
using AlertSystem.Utils.Database;
using AlertSystem.Utils.DependencyInjection;
using AlertSystem.Utils.Logging;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
builder.Configuration.LoadEnvironmentVariables();
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog
builder.Host.AddSerilogConfiguration("AlertSystem.API");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => AlertSystem.API.SwaggerConfiguration.ConfigureSwagger(c));

// Add AlertSystem services
builder.Services.AddApiServices(builder.Configuration);

// Add database services (prefer appsettings over env to avoid unexpected overrides)
// Read strictly from appsettings files to avoid .env overriding DB target
var envName = builder.Environment.EnvironmentName ?? "Production";
var fileConfig = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true)
    .Build();
var connectionString =
    fileConfig.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=AlertDB;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<AlertSystem.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IDbContext, AlertSystem.Data.ApplicationDbContext>();
builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add API Key services
builder.Services.AddScoped<IApiClientRepository, ApiClientRepository>();
builder.Services.AddScoped<IApiKeyValidator, ApiKeyValidator>();
builder.Services.AddScoped<IHotelUserRepository, HotelUserRepository>();

// Add Alert System services
builder.Services.AddScoped<AlertSystem.Service.Services.AlertSendService>();
builder.Services.AddScoped<AlertSystem.Service.Services.AlertReadService>();
builder.Services.AddScoped<AlertSystem.Service.Services.AlertCrudService>();
builder.Services.AddScoped<AlertSystem.Service.Services.AlertAuditService>();
builder.Services.AddScoped<AlertSystem.Service.Services.KpiUpdateService>();
builder.Services.AddScoped<AlertSystem.Service.Services.ConfirmationTokenService>(provider => 
    new AlertSystem.Service.Services.ConfirmationTokenService("your-secret-key-here"));
builder.Services.AddScoped<AlertSystem.Service.Services.NotificationService>();
builder.Services.AddScoped<AlertSystem.Services.INotificationService, AlertSystem.Service.Services.NotificationService>();
builder.Services.AddScoped<AlertSystem.Service.IAlertReadService, AlertSystem.Service.Services.AlertReadService>();
builder.Services.AddScoped<AlertSystem.Service.IAlertCrudService, AlertSystem.Service.Services.AlertCrudService>();
builder.Services.AddScoped<AlertSystem.Service.IKpiUpdateService, AlertSystem.Service.Services.KpiUpdateService>();
builder.Services.AddScoped<AlertSystem.Services.IWhatsAppService, AlertSystem.Services.WhatsAppService>();
builder.Services.AddScoped<AlertSystem.Service.IEmailSender, AlertSystem.Services.SmtpEmailSender>();
builder.Services.AddScoped<AlertSystem.Service.Services.IEmailTemplateService, AlertSystem.Service.Services.EmailTemplateService>();
builder.Services.AddScoped<AlertSystem.Service.Services.IWhatsAppTemplateService, AlertSystem.Service.Services.WhatsAppTemplateService>();
builder.Services.AddScoped<AlertSystem.Services.IWebPushService, AlertSystem.Services.WebPushService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Add SignalR for KpiUpdateService
builder.Services.AddSignalR();

var app = builder.Build();

// Configure Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AlertSystem API v1");
        c.RoutePrefix = string.Empty; // Swagger UI Ã  la racine
        c.DocumentTitle = "AlertSystem API Documentation";
        c.DefaultModelsExpandDepth(-1); // Masquer les modÃ¨les par dÃ©faut
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
    });
}

app.UseHttpsRedirection();

// API Key protection
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

// Auto-seed disabled: schema is created via scripts/seed-core-data.sql

app.Run();

