using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AlertSystem.Worker;
using AlertSystem.Worker.Services;
using Serilog;
DotNetEnv.Env.Load();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true).Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddLogging(lb => lb.AddSerilog());

builder.Services.AddWindowsService(options => options.ServiceName = "AlertSystem Alert Worker");

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IWhatsAppSender, WhatsAppSender>();
builder.Services.AddScoped<IWebPushNotifier, WebPushNotifier>();

// Register the new polling worker
builder.Services.AddHostedService<AlertePollingWorker>();

var host = builder.Build();
await host.RunAsync();
