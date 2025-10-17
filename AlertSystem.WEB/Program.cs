using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

// Add EF DbContext for services (before Build)
builder.Services.AddDbContext<AlertSystem.Data.ApplicationDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(conn);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// DbContext configured above

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Set Dashboard as default landing page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
