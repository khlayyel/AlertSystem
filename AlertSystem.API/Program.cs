using Microsoft.EntityFrameworkCore;
using AlertSystem.Service;
using AlertSystem.API.Middleware;
using AlertSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// DbContext
builder.Services.AddDbContext<AlertSystem.Data.ApplicationDbContext>(opts =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
    opts.UseSqlServer(cs);
});

// DI registrations
builder.Services.AddScoped<AlertSystem.Services.INotificationService, NotificationService>();
builder.Services.AddScoped<AlertSystem.Service.IEmailSender, AlertSystem.Services.SmtpEmailSender>();
builder.Services.AddHttpClient<AlertSystem.Services.WhatsAppService>();
builder.Services.AddScoped<AlertSystem.Services.IWhatsAppService, AlertSystem.Services.WhatsAppService>();
builder.Services.AddScoped<IAlertSendService, AlertSendService>();
builder.Services.AddScoped<IApiKeyValidator, ApiKeyValidator>();

var app = builder.Build();

// Swagger optional; disabled if package not referenced

app.UseHttpsRedirection();

// API Key protection
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();
