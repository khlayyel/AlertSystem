using Microsoft.Extensions.Logging;

namespace AlertSystem.Utils.Logging
{
    /// <summary>
    /// Centralized error logging patterns to eliminate duplication across projects
    /// </summary>
    public static class ErrorLoggingExtensions
    {
        /// <summary>
        /// Logs service errors with consistent formatting
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="operation">Operation being performed</param>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="context">Additional context information</param>
        public static void LogServiceError(this ILogger logger, string serviceName, string operation, Exception exception, object? context = null)
        {
            logger.LogError(exception, 
                "❌ {ServiceName} Error in {Operation}: {ErrorMessage} | Context: {@Context}", 
                serviceName, operation, exception.Message, context);
        }

        /// <summary>
        /// Logs service errors with consistent formatting (without exception)
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="operation">Operation being performed</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="context">Additional context information</param>
        public static void LogServiceError(this ILogger logger, string serviceName, string operation, string errorMessage, object? context = null)
        {
            logger.LogError("❌ {ServiceName} Error in {Operation}: {ErrorMessage} | Context: {@Context}", 
                serviceName, operation, errorMessage, context);
        }

        /// <summary>
        /// Logs service warnings with consistent formatting
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="operation">Operation being performed</param>
        /// <param name="warningMessage">Warning message</param>
        /// <param name="context">Additional context information</param>
        public static void LogServiceWarning(this ILogger logger, string serviceName, string operation, string warningMessage, object? context = null)
        {
            logger.LogWarning("⚠️ {ServiceName} Warning in {Operation}: {WarningMessage} | Context: {@Context}", 
                serviceName, operation, warningMessage, context);
        }

        /// <summary>
        /// Logs service information with consistent formatting
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="operation">Operation being performed</param>
        /// <param name="message">Information message</param>
        /// <param name="context">Additional context information</param>
        public static void LogServiceInfo(this ILogger logger, string serviceName, string operation, string message, object? context = null)
        {
            logger.LogInformation("ℹ️ {ServiceName} {Operation}: {Message} | Context: {@Context}", 
                serviceName, operation, message, context);
        }

        /// <summary>
        /// Logs service success with consistent formatting
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="operation">Operation being performed</param>
        /// <param name="message">Success message</param>
        /// <param name="context">Additional context information</param>
        public static void LogServiceSuccess(this ILogger logger, string serviceName, string operation, string message, object? context = null)
        {
            logger.LogInformation("✅ {ServiceName} Success in {Operation}: {Message} | Context: {@Context}", 
                serviceName, operation, message, context);
        }

        /// <summary>
        /// Logs database operations with consistent formatting
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="operation">Database operation</param>
        /// <param name="entityType">Type of entity being operated on</param>
        /// <param name="entityId">ID of the entity</param>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="duration">Duration of the operation</param>
        public static void LogDatabaseOperation(this ILogger logger, string operation, string entityType, object? entityId, bool success, TimeSpan? duration = null)
        {
            var level = success ? LogLevel.Information : LogLevel.Error;
            var icon = success ? "✅" : "❌";
            var durationText = duration.HasValue ? $" | Duration: {duration.Value.TotalMilliseconds}ms" : "";

            logger.Log(level, "{Icon} Database {Operation} on {EntityType} (ID: {EntityId}) - {(success ? \"Success\" : \"Failed\")}{DurationText}", 
                icon, operation, entityType, entityId, durationText);
        }

        /// <summary>
        /// Logs API calls with consistent formatting
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="apiName">Name of the API</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="method">HTTP method</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="duration">Duration of the call</param>
        /// <param name="success">Whether the call was successful</param>
        public static void LogApiCall(this ILogger logger, string apiName, string endpoint, string method, int statusCode, TimeSpan duration, bool success)
        {
            var level = success ? LogLevel.Information : LogLevel.Warning;
            var icon = success ? "✅" : "⚠️";

            logger.Log(level, "{Icon} {ApiName} API Call: {Method} {Endpoint} - Status: {StatusCode} | Duration: {Duration}ms", 
                icon, apiName, method, endpoint, statusCode, duration.TotalMilliseconds);
        }
    }
}
