using Microsoft.EntityFrameworkCore;
using AlertSystem.Service;
using AlertSystem.API.Middleware;
using AlertSystem.Services;
using Serilog;
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// DbContext
builder.Services.AddDbContext<AlertSystem.Data.ApplicationDbContext>(opts =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
    opts.UseSqlServer(cs);
});

// Register IDbContext abstraction
builder.Services.AddScoped<AlertSystem.DataLayer.Interfaces.IDbContext>(provider => 
    provider.GetRequiredService<AlertSystem.Data.ApplicationDbContext>());

// DI registrations
builder.Services.AddScoped<AlertSystem.Services.INotificationService, NotificationService>();
builder.Services.AddScoped<AlertSystem.Service.IEmailSender, AlertSystem.Services.SmtpEmailSender>();
builder.Services.AddScoped<AlertSystem.Service.IEmailTemplateService, AlertSystem.Service.EmailTemplateService>();
builder.Services.AddScoped<ConfirmationTokenService>(provider =>
    new ConfirmationTokenService(provider.GetRequiredService<IConfiguration>()?["TOKEN_SECRET"] ?? "dev-secret-change-me"));
builder.Services.AddHttpClient<AlertSystem.Services.WhatsAppService>();
builder.Services.AddScoped<AlertSystem.Services.IWhatsAppService, AlertSystem.Services.WhatsAppService>();
builder.Services.AddScoped<IAlertSendService, AlertSendService>();
builder.Services.AddScoped<IApiKeyValidator, ApiKeyValidator>();

// Register Repository Interfaces
builder.Services.AddScoped<IAlerteRepository, AlerteRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IHistoriqueAlerteRepository, HistoriqueAlerteRepository>();
builder.Services.AddScoped<IRappelSuivantRepository, RappelSuivantRepository>();
builder.Services.AddScoped<IApiClientRepository, ApiClientRepository>();
builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();

// Swagger optional; disabled if package not referenced

app.UseHttpsRedirection();

// API Key protection
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

// Auto-seed reference data on startup (idempotent)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AlertSystem.Data.ApplicationDbContext>();
    await AlertSystem.DbSeeder.SeedAsync(db);
}

app.Run();
