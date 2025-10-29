using AlertSystem.Worker;
using AlertSystem.Data;
using AlertSystem.Service.Services;
using AlertSystem.Service;
using AlertSystem.Services;
using AlertSystem.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using DotNetEnv;
using Microsoft.AspNetCore.SignalR;

// Load solution root .env (one level above the project directory)
var rootEnvPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));
if (File.Exists(rootEnvPath))
{
    Env.Load(rootEnvPath);
}
else
{
    // Fallback: try current directory
    Env.Load();
}

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting AlertSystem.Worker");

    var builder = Host.CreateApplicationBuilder(args);
    
    // Configure Serilog
    builder.Services.AddSerilog();
    
    // Configure Windows Service
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "AlertSystem.Worker";
    });

    // Configure Database (prefer .env root key CONNECTIONSTRINGS__DEFAULTCONNECTION)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var envConn = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION");
        var connectionString = envConn
            ?? builder.Configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=BELVEDERE_17_10_2025;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        Log.Information("Worker using connection string: {ConnectionString}", connectionString);
        options.UseSqlServer(connectionString);
    });

    // Register IDbContext interface
    builder.Services.AddScoped<AlertSystem.DataLayer.Interfaces.IDbContext>(sp => 
        sp.GetRequiredService<ApplicationDbContext>());

    // Register Services
    builder.Services.AddScoped<AlertReadService>();
    builder.Services.AddScoped<AlertCrudService>();
    builder.Services.AddScoped<AlertSendService>();
    builder.Services.AddScoped<AlertAuditService>();
    builder.Services.AddScoped<ConfirmationTokenService>();
    builder.Services.AddScoped<KpiUpdateService>();
    builder.Services.AddScoped<AlertSystem.DataLayer.Interfaces.IHotelUserRepository, AlertSystem.Repository.Implementations.HotelUserRepository>();
    
    // Register interface dependencies
    builder.Services.AddScoped<AlertSystem.Service.IEmailSender, AlertSystem.Services.SmtpEmailSender>();
    builder.Services.AddScoped<AlertSystem.Service.IAlertReadService>(sp => sp.GetRequiredService<AlertReadService>());
    builder.Services.AddScoped<AlertSystem.Service.IAlertCrudService>(sp => sp.GetRequiredService<AlertCrudService>());
    builder.Services.AddScoped<AlertSystem.Service.IKpiUpdateService, KpiUpdateService>();
    
    // Register notification services
    builder.Services.AddScoped<AlertSystem.Services.INotificationService, AlertSystem.Service.Services.NotificationService>();
    builder.Services.AddScoped<AlertSystem.Service.Services.IEmailTemplateService, EmailTemplateService>();
    builder.Services.AddScoped<AlertSystem.Service.Services.IWhatsAppTemplateService, WhatsAppTemplateService>();
    builder.Services.AddScoped<AlertSystem.Services.IWhatsAppService, AlertSystem.Services.WhatsAppService>();
    builder.Services.AddScoped<AlertSystem.Services.IWebPushService, AlertSystem.Services.WebPushService>();
    
    // Register IHttpContextAccessor (needed by AlertSendService)
    builder.Services.AddHttpContextAccessor();
    
    // Register HttpClient
    builder.Services.AddHttpClient();
    
    // Register configuration for ConfirmationTokenService
    builder.Services.AddSingleton(provider => "test-secret-key");
    
    // Register IAlertSendService interface
    builder.Services.AddScoped<AlertSystem.Service.Interfaces.IAlertSendService>(sp => sp.GetRequiredService<AlertSendService>());

    // Register SignalR (for IHubContext usage in worker)
    builder.Services.AddSignalR();

    // Register Worker Service
    builder.Services.AddHostedService<ConsolidatedWorkerService>();

    var host = builder.Build();
    
    Log.Information("AlertSystem.Worker started successfully");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AlertSystem.Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}


