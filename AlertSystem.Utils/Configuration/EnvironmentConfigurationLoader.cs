using Microsoft.Extensions.Configuration;
using AlertSystem.Utils.Configuration;

namespace AlertSystem.Utils.Configuration
{
    /// <summary>
    /// Single Responsibility: Load environment variables from .env file
    /// DRY Principle: Centralized environment configuration loading
    /// </summary>
    public static class EnvironmentConfigurationLoader
    {
        /// <summary>
        /// Loads environment variables from .env file and adds them to the configuration builder
        /// </summary>
        /// <param name="builder">Configuration builder</param>
        /// <returns>Updated configuration builder</returns>
        public static IConfigurationBuilder LoadEnvironmentVariables(this IConfigurationBuilder builder)
        {
            // DRY Principle: Load .env file from solution root (not project-specific)
            var solutionRoot = FindSolutionRoot();
            var envPath = Path.Combine(solutionRoot, ".env");
            
            Console.WriteLine($"🔧 ENV DEBUG: Looking for .env file at: {envPath}");
            Console.WriteLine($"🔧 ENV DEBUG: File exists: {File.Exists(envPath)}");
            
            if (File.Exists(envPath))
            {
                Console.WriteLine($"🔧 ENV DEBUG: Loading .env file from: {envPath}");
                DotNetEnv.Env.Load(envPath);
                
                // Check if SMTP_PORT is loaded
                var smtpPort = Environment.GetEnvironmentVariable("Smtp__Port");
                Console.WriteLine($"🔧 ENV DEBUG: Smtp__Port from environment: {smtpPort}");
                
                // Add environment variables to configuration
                builder.AddEnvironmentVariables();
            }
            else
            {
                Console.WriteLine($"🔧 ENV DEBUG: .env file not found at: {envPath}");
            }
            
            return builder;
        }
        
        private static string FindSolutionRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(currentDir);
            
            // Look for .sln file in current directory or parent directories
            while (dir != null)
            {
                if (dir.GetFiles("*.sln").Length > 0)
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            
            // Fallback to current directory if no solution found
            return currentDir;
        }
    }
}
