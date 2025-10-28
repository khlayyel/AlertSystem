using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AlertSystem.Utils.Configuration;
using AlertSystem.Utils.Logging;

namespace AlertSystem.Utils.Email
{
    /// <summary>
    /// Centralized SMTP configuration to eliminate duplication across projects
    /// </summary>
    public static class SmtpConfigurationUtils
    {
        /// <summary>
        /// Gets SMTP configuration from environment variables and configuration
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <returns>SMTP configuration object</returns>
        public static SmtpConfiguration GetSmtpConfiguration(IConfiguration configuration)
        {
            var host = configuration[ConfigurationConstants.SMTP_HOST_KEY] ?? 
                       Environment.GetEnvironmentVariable(ConfigurationConstants.SMTP_HOST_KEY) ?? 
                       ConfigurationConstants.DEFAULT_SMTP_HOST;
            
            var portString = configuration[ConfigurationConstants.SMTP_PORT_KEY] ?? 
                             Environment.GetEnvironmentVariable(ConfigurationConstants.SMTP_PORT_KEY);
            var port = int.TryParse(portString, out var parsedPort) ? parsedPort : ConfigurationConstants.DEFAULT_SMTP_PORT;
            
            var user = configuration[ConfigurationConstants.SMTP_USER_KEY] ?? 
                       Environment.GetEnvironmentVariable(ConfigurationConstants.SMTP_USER_KEY) ?? 
                       string.Empty;
            
            var password = configuration[ConfigurationConstants.SMTP_PASSWORD_KEY] ?? 
                           Environment.GetEnvironmentVariable(ConfigurationConstants.SMTP_PASSWORD_KEY) ?? 
                           string.Empty;
            
            var useStartTls = string.Equals(
                configuration[ConfigurationConstants.SMTP_USE_START_TLS_KEY] ?? 
                Environment.GetEnvironmentVariable(ConfigurationConstants.SMTP_USE_START_TLS_KEY), 
                "true", StringComparison.OrdinalIgnoreCase) || 
                ConfigurationConstants.DEFAULT_USE_START_TLS;
            
            var useSsl = string.Equals(
                configuration[ConfigurationConstants.SMTP_USE_SSL_KEY] ?? 
                Environment.GetEnvironmentVariable(ConfigurationConstants.SMTP_USE_SSL_KEY), 
                "true", StringComparison.OrdinalIgnoreCase) || 
                ConfigurationConstants.DEFAULT_USE_SSL;
            
            var fromName = configuration["Smtp:FromName"] ?? "AlertSystem";
            var fromEmail = configuration["Smtp:From"] ?? "no-reply@example.com";
            
            // Debug logging
            Console.WriteLine($"🔧 SMTP CONFIG DEBUG:");
            Console.WriteLine($"  Host: {host}");
            Console.WriteLine($"  Port: {port} (from string: '{portString}')");
            Console.WriteLine($"  User: {user}");
            Console.WriteLine($"  Password: {(string.IsNullOrEmpty(password) ? "EMPTY" : "SET")}");
            Console.WriteLine($"  UseStartTls: {useStartTls}");
            Console.WriteLine($"  UseSsl: {useSsl}");
            Console.WriteLine($"  FromName: {fromName}");
            Console.WriteLine($"  FromEmail: {fromEmail}");
            
            return new SmtpConfiguration
            {
                Host = host,
                Port = port,
                User = user,
                Password = password,
                UseStartTls = useStartTls,
                UseSsl = useSsl,
                FromName = fromName,
                FromEmail = fromEmail
            };
        }

        /// <summary>
        /// Determines the appropriate TLS option based on port and configuration
        /// </summary>
        /// <param name="config">SMTP configuration</param>
        /// <returns>SecureSocketOptions for the connection</returns>
        public static SecureSocketOptions GetTlsOptions(SmtpConfiguration config)
        {
            // Choose the right TLS mode. Gmail typically requires:
            // - Port 465: SSL on connect
            // - Port 587: STARTTLS
            if (config.Port == 465)
            {
                return SecureSocketOptions.SslOnConnect;
            }
            else if (config.UseStartTls)
            {
                return SecureSocketOptions.StartTls;
            }
            else if (config.UseSsl)
            {
                return SecureSocketOptions.SslOnConnect;
            }
            else
            {
                return SecureSocketOptions.Auto;
            }
        }

        /// <summary>
        /// Creates and configures an SMTP client with the provided configuration
        /// </summary>
        /// <param name="config">SMTP configuration</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>Configured SMTP client</returns>
        public static SmtpClient CreateSmtpClient(SmtpConfiguration config, ILogger logger)
        {
            var client = new SmtpClient();
            var tlsOption = GetTlsOptions(config);

            logger.LogServiceInfo("SmtpConfigurationUtils", "CreateSmtpClient", 
                $"Creating SMTP client for {config.Host}:{config.Port} with TLS: {tlsOption}");

            return client;
        }

        /// <summary>
        /// Connects and authenticates an SMTP client
        /// </summary>
        /// <param name="client">SMTP client</param>
        /// <param name="config">SMTP configuration</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>Task representing the connection operation</returns>
        public static async Task ConnectAndAuthenticateAsync(SmtpClient client, SmtpConfiguration config, ILogger logger)
        {
            var tlsOption = GetTlsOptions(config);

            try
            {
                await client.ConnectAsync(config.Host, config.Port, tlsOption);
                
                // Authenticate if username is provided (supports Gmail/app passwords)
                if (!string.IsNullOrEmpty(config.User))
                {
                    await client.AuthenticateAsync(config.User, config.Password);
                    logger.LogServiceInfo("SmtpConfigurationUtils", "ConnectAndAuthenticate", 
                        "SMTP authentication successful");
                }
                else
                {
                    logger.LogServiceWarning("SmtpConfigurationUtils", "ConnectAndAuthenticate", 
                        "No SMTP credentials provided, using unauthenticated connection");
                }
            }
            catch (Exception ex)
            {
                logger.LogServiceError("SmtpConfigurationUtils", "ConnectAndAuthenticate", ex, new
                {
                    Host = config.Host,
                    Port = config.Port,
                    User = config.User,
                    UseStartTls = config.UseStartTls,
                    UseSsl = config.UseSsl,
                    TlsOption = tlsOption
                });
                throw;
            }
        }

        /// <summary>
        /// Safely disconnects an SMTP client
        /// </summary>
        /// <param name="client">SMTP client</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>Task representing the disconnection operation</returns>
        public static async Task SafeDisconnectAsync(SmtpClient client, ILogger logger)
        {
            try
            {
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                logger.LogServiceWarning("SmtpConfigurationUtils", "SafeDisconnect", 
                    $"Error during SMTP disconnect: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// SMTP configuration model
    /// </summary>
    public class SmtpConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseStartTls { get; set; } = true;
        public bool UseSsl { get; set; } = false;
        public string FromName { get; set; } = "AlertSystem";
        public string FromEmail { get; set; } = "no-reply@example.com";
    }
}
