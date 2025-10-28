using DotNetEnv;

namespace AlertSystem.Utils.Configuration
{
    /// <summary>
    /// Centralized environment variable loading to eliminate duplication across projects
    /// </summary>
    public static class EnvironmentExtensions
    {
        /// <summary>
        /// Loads environment variables from .env file with validation
        /// </summary>
        public static void LoadEnvironmentVariables()
        {
            try
            {
                Env.Load();
            }
            catch (Exception ex)
            {
                // Log warning but don't fail - environment variables might be set via other means
                Console.WriteLine($"Warning: Could not load .env file: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that required environment variables are present
        /// </summary>
        public static void ValidateRequiredEnvironmentVariables()
        {
            var requiredVars = new[]
            {
                ConfigurationConstants.TOKEN_SECRET_KEY,
                ConfigurationConstants.SMTP_HOST_KEY,
                ConfigurationConstants.SMTP_USER_KEY,
                ConfigurationConstants.SMTP_PASSWORD_KEY,
                ConfigurationConstants.WHATSAPP_ACCESS_TOKEN_KEY,
                ConfigurationConstants.WHATSAPP_PHONE_NUMBER_ID_KEY
            };

            var missingVars = new List<string>();
            
            foreach (var varName in requiredVars)
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(varName)))
                {
                    missingVars.Add(varName);
                }
            }

            if (missingVars.Any())
            {
                throw new InvalidOperationException(
                    $"Missing required environment variables: {string.Join(", ", missingVars)}");
            }
        }
    }
}
