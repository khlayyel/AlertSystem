using AlertSystem.WatcherWorker;
using AlertSystem.Data;
using AlertSystem.Service.Services;
using AlertSystem.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/watcher-worker-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting AlertSystem.WatcherWorker");

    var builder = Host.CreateApplicationBuilder(args);
    
    // Configure Serilog
    builder.Services.AddSerilog();
    
    // Configure Windows Service
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "AlertSystem.WatcherWorker";
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
    builder.Services.AddScoped<AlertCrudService>();
    builder.Services.AddScoped<AlertReadService>();
    builder.Services.AddScoped<AlertAuditService>();
    builder.Services.AddScoped<AlertSystem.DataLayer.Interfaces.IHotelUserRepository, AlertSystem.Repository.Implementations.HotelUserRepository>();

    // Shared infra
    builder.Services.AddSingleton<AlertSystem.WatcherWorker.Services.ITimeProvider, AlertSystem.WatcherWorker.Services.SystemTimeProvider>();
    builder.Services.AddSingleton<AlertSystem.WatcherWorker.Services.IAppResolver, AlertSystem.WatcherWorker.Services.AppResolver>();
    builder.Services.AddSingleton<AlertSystem.WatcherWorker.Services.ICapabilityResolver, AlertSystem.WatcherWorker.Services.CapabilityResolver>();
    builder.Services.AddScoped<AlertSystem.WatcherWorker.Services.IRecipientResolver, AlertSystem.WatcherWorker.Services.RecipientResolver>();
    builder.Services.AddScoped<AlertSystem.WatcherWorker.Services.IAlertWriter, AlertSystem.WatcherWorker.Services.AlertWriter>();

    // Domain watchers
    builder.Services.AddHostedService<AlertSystem.WatcherWorker.Watchers.ReservationsWatcher>();
    builder.Services.AddHostedService<AlertSystem.WatcherWorker.Watchers.TpvBillingWatcher>();
    builder.Services.AddHostedService<AlertSystem.WatcherWorker.Watchers.HrWatcher>();
    builder.Services.AddHostedService<AlertSystem.WatcherWorker.Watchers.EventsWatcher>();
    builder.Services.AddHostedService<AlertSystem.WatcherWorker.Watchers.TreasuryWatcher>();

    // No intermediary queue service required anymore

    var host = builder.Build();
    
    Log.Information("AlertSystem.WatcherWorker started successfully");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AlertSystem.WatcherWorker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
