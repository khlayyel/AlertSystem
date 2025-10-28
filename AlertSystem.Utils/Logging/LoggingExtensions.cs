using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AlertSystem.Utils.Configuration;

namespace AlertSystem.Utils.Logging
{
    /// <summary>
    /// Centralized Serilog configuration to eliminate duplication across projects
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Configures Serilog with consistent settings across all projects
        /// </summary>
        public static void AddSerilogConfiguration(this IHostBuilder host, string projectName)
        {
            var logLevel = Environment.GetEnvironmentVariable(ConfigurationConstants.LOG_LEVEL_KEY) ?? 
                          ConfigurationConstants.DEFAULT_LOG_LEVEL;

            var logLevelEnum = Enum.TryParse<LogEventLevel>(logLevel, true, out var level) 
                ? level 
                : LogEventLevel.Information;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevelEnum)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Project", projectName)
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: $"logs/{projectName}-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .CreateLogger();

            host.UseSerilog();
        }

        /// <summary>
        /// Configures Serilog for Worker services with specific settings
        /// </summary>
        public static void AddWorkerSerilogConfiguration(this IHostBuilder host, string workerName)
        {
            var logLevel = Environment.GetEnvironmentVariable(ConfigurationConstants.LOG_LEVEL_KEY) ?? 
                          ConfigurationConstants.DEFAULT_LOG_LEVEL;

            var logLevelEnum = Enum.TryParse<LogEventLevel>(logLevel, true, out var level) 
                ? level 
                : LogEventLevel.Information;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevelEnum)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Worker", workerName)
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: $"logs/{workerName}-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .CreateLogger();

            host.UseSerilog();
        }
    }
}
