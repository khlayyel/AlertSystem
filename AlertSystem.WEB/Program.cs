using Microsoft.EntityFrameworkCore;
using Serilog;
using Hangfire;
using Hangfire.SqlServer;
using AlertSystem.WEB.Services;
using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Repository.Implementations;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true).Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
builder.Host.UseSerilog();
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllersWithViews();

// TODO: Register application services used by migrated controllers
// e.g. builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<AlertSystem.Service.IAlertReadService, AlertSystem.Service.AlertReadService>();
builder.Services.AddScoped<AlertSystem.Service.IAlertCrudService, AlertSystem.Service.AlertCrudService>();
builder.Services.AddScoped<AlertSystem.Service.IEmailSender, AlertSystem.Services.SmtpEmailSender>();
builder.Services.AddScoped<AlertSystem.Services.INotificationService, AlertSystem.Service.NotificationService>();
builder.Services.AddHttpClient<AlertSystem.Services.WhatsAppService>();
builder.Services.AddScoped<AlertSystem.Services.IWhatsAppService, AlertSystem.Services.WhatsAppService>();
// WebPush abstraction if used by PushController (currently no-op wrapper over infra API controllers)
builder.Services.AddScoped<AlertSystem.Services.IWebPushService, AlertSystem.Services.WebPushService>();
builder.Services.AddScoped<AlertSystem.Service.AlertAuditService>();
builder.Services.AddSingleton<AlertSystem.Service.ReminderConfiguration>();
builder.Services.AddScoped<AlertSystem.Service.IAlertSendService, AlertSystem.Service.AlertSendService>();
builder.Services.AddScoped<AlertSystem.Service.ConfirmationTokenService>(provider => 
    new AlertSystem.Service.ConfirmationTokenService(provider.GetRequiredService<IConfiguration>()["TOKEN_SECRET"] ?? "dev-secret-change-me"));
builder.Services.AddScoped<AlertSystem.Service.IEmailTemplateService, AlertSystem.Service.EmailTemplateService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AlertSystem.Services.ICurrentUserAccessor, AlertSystem.Services.CurrentUserAccessor>();
builder.Services.AddScoped<IDelayedAlertJobService, DelayedAlertJobService>();

// Register Repository Interfaces
builder.Services.AddScoped<IAlerteRepository, AlerteRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IHistoriqueAlerteRepository, HistoriqueAlerteRepository>();
builder.Services.AddScoped<IRappelSuivantRepository, RappelSuivantRepository>();
builder.Services.AddScoped<IApiClientRepository, ApiClientRepository>();
builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add EF DbContext for services (before Build)
builder.Services.AddDbContext<AlertSystem.Data.ApplicationDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(conn);
});

// Register IDbContext abstraction
builder.Services.AddScoped<AlertSystem.DataLayer.Interfaces.IDbContext>(provider => 
    provider.GetRequiredService<AlertSystem.Data.ApplicationDbContext>());

// Add Hangfire for background jobs
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Auto-seed reference data on startup (idempotent)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AlertSystem.Data.ApplicationDbContext>();
    await AlertSystem.DbSeeder.SeedAsync(db);
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Redirect root to Inbox to avoid 404 on /
app.MapGet("/", () => Results.Redirect("/Dashboard/Inbox"));

// Set Dashboard as default landing page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
