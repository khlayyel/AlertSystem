using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AlertSystem.Utils.Configuration;
using AlertSystem.Utils.Database;
using AlertSystem.Utils.Logging;
using AlertSystem.Utils.Validation;
using AlertSystem.Utils.WhatsApp;

namespace AlertSystem.Utils.DependencyInjection
{
    /// <summary>
    /// Centralized dependency injection registration to eliminate duplication across projects
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all AlertSystem services for WEB/API projects
        /// Note: This method provides the registration pattern
        /// The actual service registrations should be done in the consuming project
        /// to avoid circular dependencies
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddAlertSystemServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Database
            services.AddAlertSystemDatabase(configuration);

            // Note: Service registrations should be implemented in the consuming project
            // to avoid circular dependencies

            return services;
        }

        /// <summary>
        /// Registers all AlertSystem services for Worker projects
        /// Note: This method provides the registration pattern
        /// The actual service registrations should be done in the consuming project
        /// to avoid circular dependencies
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Database
            services.AddWorkerDatabase(configuration);

            // Note: Service registrations should be implemented in the consuming project
            // to avoid circular dependencies

            return services;
        }

        /// <summary>
        /// Registers API-specific services
        /// Note: This method provides the registration pattern
        /// The actual service registrations should be done in the consuming project
        /// to avoid circular dependencies
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add base services
            services.AddAlertSystemServices(configuration);

            // Note: Service registrations should be implemented in the consuming project
            // to avoid circular dependencies

            return services;
        }

        /// <summary>
        /// Registers WEB-specific services
        /// Note: This method provides the registration pattern
        /// The actual service registrations should be done in the consuming project
        /// to avoid circular dependencies
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add base services
            services.AddAlertSystemServices(configuration);

            // Note: Service registrations should be implemented in the consuming project
            // to avoid circular dependencies

            return services;
        }

        /// <summary>
        /// Registers Hangfire services
        /// Note: This method provides the registration pattern
        /// The actual service registrations should be done in the consuming project
        /// to avoid circular dependencies
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                                  Environment.GetEnvironmentVariable(ConfigurationConstants.HANGFIRE_CONNECTION_STRING_KEY) ??
                                  ConfigurationConstants.DEFAULT_HANGFIRE_CONNECTION_STRING;

            // Note: Hangfire registration should be implemented in the consuming project
            // to avoid circular dependencies

            return services;
        }

        /// <summary>
        /// Registers SignalR services
        /// Note: This method provides the registration pattern
        /// The actual service registrations should be done in the consuming project
        /// to avoid circular dependencies
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddSignalRServices(this IServiceCollection services)
        {
            // Note: SignalR registration should be implemented in the consuming project
            // to avoid circular dependencies

            return services;
        }

        /// <summary>
        /// Registers authentication services
        /// Note: This method provides the registration pattern
        /// The actual service registrations should be done in the consuming project
        /// to avoid circular dependencies
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
        {
            var tokenSecret = Environment.GetEnvironmentVariable(ConfigurationConstants.TOKEN_SECRET_KEY) ?? 
                             ConfigurationConstants.DEFAULT_TOKEN_SECRET;

            // Note: Authentication registration should be implemented in the consuming project
            // to avoid circular dependencies

            return services;
        }

        /// <summary>
        /// Registers CORS services
        /// Note: This method provides the registration pattern
        /// The actual service registrations should be done in the consuming project
        /// to avoid circular dependencies
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddCorsServices(this IServiceCollection services)
        {
            // Note: CORS registration should be implemented in the consuming project
            // to avoid circular dependencies

            return services;
        }
    }
}
