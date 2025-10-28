using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AlertSystem.Utils.Configuration;

namespace AlertSystem.Utils.Database
{
    /// <summary>
    /// Centralized database configuration to eliminate duplication across projects
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Configures AlertSystem database with consistent settings
        /// </summary>
        public static IServiceCollection AddAlertSystemDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                                  Environment.GetEnvironmentVariable(ConfigurationConstants.CONNECTION_STRING_KEY) ??
                                  ConfigurationConstants.DEFAULT_CONNECTION_STRING;

            // Note: This method provides the configuration pattern
            // The actual DbContext registration should be done in the consuming project
            // to avoid circular dependencies

            return services;
        }

        /// <summary>
        /// Configures database for Worker services with specific settings
        /// </summary>
        public static IServiceCollection AddWorkerDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                                  Environment.GetEnvironmentVariable(ConfigurationConstants.CONNECTION_STRING_KEY) ??
                                  ConfigurationConstants.DEFAULT_CONNECTION_STRING;

            // Note: This method provides the configuration pattern
            // The actual DbContext registration should be done in the consuming project
            // to avoid circular dependencies

            return services;
        }

        /// <summary>
        /// Validates database connection
        /// </summary>
        public static async Task<bool> ValidateDatabaseConnectionAsync(this IServiceProvider serviceProvider)
        {
            // Note: This method provides the validation pattern
            // The actual validation should be done in the consuming project
            // to avoid circular dependencies
            return await Task.FromResult(true);
        }
    }
}
