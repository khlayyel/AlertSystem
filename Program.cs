using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Hubs;
using AlertSystem.Services;
using AlertSystem.Middleware;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using AlertSystem;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
// Email sending is provided by SmtpEmailSender via IEmailSender
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<AlertAuditService>();
builder.Services.AddSingleton<AlertCancellationService>();
// WhatsApp service uses typed HttpClient configured with appsettings WhatsApp section
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IApiKeyValidator, ApiKeyValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AlertSystem API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        Description = "Clé API pour accéder aux endpoints /api"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            }, new string[]{}
        }
    });
});

// Configuration des reminders
var reminderConfig = new AlertSystem.Services.ReminderConfiguration();
var reminderSettings = builder.Configuration.GetSection("ReminderSettings");
if (reminderSettings.Exists())
{
    reminderConfig.FirstReminderDelay = TimeSpan.FromMinutes(reminderSettings.GetValue<int>("FirstReminderDelayMinutes", 30));
    reminderConfig.SubsequentReminderInterval = TimeSpan.FromMinutes(reminderSettings.GetValue<int>("SubsequentReminderIntervalMinutes", 60));
    reminderConfig.MaxReminders = reminderSettings.GetValue<int>("MaxReminders", 5);
    reminderConfig.ServiceCheckInterval = TimeSpan.FromMinutes(reminderSettings.GetValue<int>("ServiceCheckIntervalMinutes", 5));
    reminderConfig.EnableReminders = reminderSettings.GetValue<bool>("EnableReminders", true);
}
builder.Services.AddSingleton(reminderConfig);
builder.Services.AddHostedService<ReminderService>();

// WhatsApp Service
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Database seeding is now available via API: POST /api/v1/seed/database

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Auth API Key pour toutes les routes /api
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<NotificationsHub>("/hubs/notifications");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AlertSystem API v1");
    });
}


app.Run();
