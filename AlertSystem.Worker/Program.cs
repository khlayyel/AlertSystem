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

    // Configure Database (prefer appsettings over env to avoid unexpected overrides)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        // Read strictly from appsettings files to prevent .env or machine env from overriding
        var envName = builder.Environment.EnvironmentName ?? "Production";
        var fileConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true)
            .Build();
        var connectionString =
            fileConfig.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=AlerteDB;Trusted_Connection=True;MultipleActiveResultSets=true";
        // Force old default 'AlertDB' to new 'AlerteDB' to avoid schema mismatch
        if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("Database=AlertDB", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = connectionString.Replace("Database=AlertDB", "Database=AlerteDB", StringComparison.OrdinalIgnoreCase);
        }

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
    // Token/confirm service with unified secret from env/config
    var tokenSecret = Environment.GetEnvironmentVariable("TOKEN_SECRET")
        ?? builder.Configuration["TOKEN_SECRET"]
        ?? "dev-secret-change-me";
    builder.Services.AddSingleton(new ConfirmationTokenService(tokenSecret));
    builder.Services.AddScoped<KpiUpdateService>();
    builder.Services.AddScoped<AlertSystem.DataLayer.Interfaces.IHotelUserRepository, AlertSystem.Repository.Implementations.HotelUserRepository>();
    
    // Register interface dependencies
    builder.Services.AddScoped<AlertSystem.Utils.Abstractions.IEmailSender, AlertSystem.Services.SmtpEmailSender>();
    builder.Services.AddScoped<AlertSystem.Service.IAlertReadService>(sp => sp.GetRequiredService<AlertReadService>());
    builder.Services.AddScoped<AlertSystem.Service.IAlertCrudService>(sp => sp.GetRequiredService<AlertCrudService>());
    builder.Services.AddScoped<AlertSystem.Service.IKpiUpdateService, KpiUpdateService>();
    
    // Register notification services
    builder.Services.AddScoped<AlertSystem.Utils.Abstractions.INotificationService, AlertSystem.Service.Services.NotificationService>();
    builder.Services.AddScoped<AlertSystem.Service.Services.IEmailTemplateService, EmailTemplateService>();
    builder.Services.AddScoped<AlertSystem.Service.Services.IWhatsAppTemplateService, WhatsAppTemplateService>();
    builder.Services.AddScoped<AlertSystem.Utils.Abstractions.IWhatsAppService, AlertSystem.Services.WhatsAppService>();
    builder.Services.AddScoped<AlertSystem.Services.IWebPushService, AlertSystem.Services.WebPushService>();
    
    // Register IHttpContextAccessor (needed by AlertSendService)
    builder.Services.AddHttpContextAccessor();
    
    // Register HttpClient
    builder.Services.AddHttpClient();
    
    // Register IAlertSendService interface
    builder.Services.AddScoped<AlertSystem.Service.Interfaces.IAlertSendService>(sp => sp.GetRequiredService<AlertSendService>());

    // Register shared polling services
    builder.Services.AddSingleton<AlertSystem.Worker.Services.IHotelApiClient, AlertSystem.Worker.Services.HotelApiClient>();
    builder.Services.AddScoped<AlertSystem.Worker.Services.IAlertInsertService, AlertSystem.Worker.Services.AlertInsertService>();
    
    // Register new services for def_alerte based polling
    builder.Services.AddScoped<AlertSystem.Worker.Services.IDefAlerteService, AlertSystem.Worker.Services.DefAlerteService>();
    builder.Services.AddScoped<AlertSystem.Worker.Services.IUserRecipientService, AlertSystem.Worker.Services.UserRecipientService>();
    builder.Services.AddSingleton<AlertSystem.Worker.Services.IAlertTypeTemplateService, AlertSystem.Worker.Services.AlertTypeTemplateService>();

    // Register domain pollers (AlertPoller now reads from def_Alerte table)
    builder.Services.AddSingleton<AlertSystem.Worker.Watchers.IHotelDomainPoller, AlertSystem.Worker.Watchers.AlertPoller>();

    // Register orchestrator and sender
    builder.Services.AddHostedService<AlertSystem.Worker.Watchers.PollingOrchestratorWorker>();
    builder.Services.AddHostedService<AlertSystem.Worker.Watchers.AlertSenderWorker>();

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
