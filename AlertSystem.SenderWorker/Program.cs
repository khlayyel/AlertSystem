using AlertSystem.SenderWorker;
using AlertSystem.Data;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using AlertSystem.Services;
using AlertSystem.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/sender-worker-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting AlertSystem.SenderWorker");

    var builder = Host.CreateApplicationBuilder(args);
    
    // Configure Serilog
    builder.Services.AddSerilog();
    
    // Configure Windows Service
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "AlertSystem.SenderWorker";
    });

    // Configure Database
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                              Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                              "Server=(localdb)\\mssqllocaldb;Database=AlertSystemDB;Trusted_Connection=true;MultipleActiveResultSets=true";
        options.UseSqlServer(connectionString);
    });

    // Register IDbContext interface
    builder.Services.AddScoped<AlertSystem.DataLayer.Interfaces.IDbContext>(sp => 
        sp.GetRequiredService<ApplicationDbContext>());

    // Register Services
    builder.Services.AddScoped<AlertSendService>();
    builder.Services.AddScoped<AlertCrudService>();
    builder.Services.AddScoped<AlertReadService>();
    builder.Services.AddScoped<AlertAuditService>();
    builder.Services.AddScoped<ConfirmationTokenService>();
    builder.Services.AddScoped<KpiUpdateService>();
    builder.Services.AddScoped<AlertSystem.DataLayer.Interfaces.IHotelUserRepository, AlertSystem.Repository.Implementations.HotelUserRepository>();
    
    // Register missing dependencies
    builder.Services.AddScoped<AlertSystem.Service.IEmailSender, AlertSystem.Services.SmtpEmailSender>();
    builder.Services.AddScoped<AlertSystem.Service.IAlertReadService>(sp => sp.GetRequiredService<AlertReadService>());
    builder.Services.AddScoped<AlertSystem.Service.IAlertCrudService>(sp => sp.GetRequiredService<AlertCrudService>());
    
    // Register configuration for ConfirmationTokenService
    builder.Services.AddSingleton(provider => "test-secret-key");

    // Register Sender Service
    builder.Services.AddHostedService<SenderService>();

    var host = builder.Build();
    
    Log.Information("AlertSystem.SenderWorker started successfully");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AlertSystem.SenderWorker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
