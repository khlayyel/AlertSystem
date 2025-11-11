using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using AlertSystem.WEB.Services;
using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Repository.Implementations;
using AlertSystem.Utils.Configuration;
using AlertSystem.Utils.Database;
using AlertSystem.Utils.DependencyInjection;
using AlertSystem.Utils.Logging;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using AlertSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
builder.Configuration.LoadEnvironmentVariables();
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog
builder.Host.AddSerilogConfiguration("AlertSystem.WEB");

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add authentication and authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Add AlertSystem services
builder.Services.AddWebServices(builder.Configuration);

// Add additional WEB-specific services
builder.Services.AddScoped<IRappelSuivantRepository, RappelSuivantRepository>();
builder.Services.AddScoped<IApiClientRepository, ApiClientRepository>();
builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IHotelUserRepository, HotelUserRepository>();

// Add missing service registrations
builder.Services.AddScoped<AlertSendService>();
builder.Services.AddScoped<AlertReadService>();
builder.Services.AddScoped<AlertCrudService>();
builder.Services.AddScoped<AlertAuditService>();
builder.Services.AddScoped<KpiUpdateService>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

// Add interface registrations
builder.Services.AddScoped<AlertSystem.Service.IAlertReadService, AlertSystem.Service.Services.AlertReadService>();
builder.Services.AddScoped<AlertSystem.Service.IAlertCrudService, AlertSystem.Service.Services.AlertCrudService>();
builder.Services.AddScoped<AlertSystem.Service.IKpiUpdateService, AlertSystem.Service.Services.KpiUpdateService>();
builder.Services.AddScoped<AlertSystem.Services.INotificationService, AlertSystem.Service.Services.NotificationService>();

// Add all required services for AlertSendService
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IWhatsAppTemplateService, WhatsAppTemplateService>();
builder.Services.AddScoped<AlertSystem.Services.IWebPushService, AlertSystem.Services.WebPushService>();
builder.Services.AddHttpContextAccessor();

// Add missing dependencies
builder.Services.AddScoped<AlertSystem.Service.IEmailSender, AlertSystem.Services.SmtpEmailSender>();
builder.Services.AddHttpClient(); // For WhatsApp services
builder.Services.AddScoped<ConfirmationTokenService>(provider => 
    new ConfirmationTokenService("your-secret-key-here"));

// Add current user service
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Add password service
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Add SignalR for KpiUpdateService
builder.Services.AddSignalR();

// Add database services (read strictly from appsettings to avoid .env overrides)
var envName = builder.Environment.EnvironmentName ?? "Production";
var fileConfig = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true)
    .Build();
var connectionString = fileConfig.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing from appsettings.");
}

builder.Services.AddDbContext<AlertSystem.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IDbContext, AlertSystem.Data.ApplicationDbContext>();

// Hangfire removed - using WatcherWorker and SenderWorker instead

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Auto-seed reference data on startup (idempotent)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AlertSystem.Data.ApplicationDbContext>();
    await AlertSystem.DbSeeder.SeedAsync(db);
}

// In development we avoid HTTPS redirection to prevent losing JSON bodies on redirects
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Configure SignalR
app.MapHub<AlertSystem.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");

app.MapStaticAssets();

// Set Home as default controller for root access
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

