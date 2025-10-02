using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Hubs;
using AlertSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<AlertAuditService>();
builder.Services.AddSingleton<AlertCancellationService>();
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<NotificationsHub>("/hubs/notifications");


app.Run();
