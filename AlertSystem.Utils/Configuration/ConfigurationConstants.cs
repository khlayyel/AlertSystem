namespace AlertSystem.Utils.Configuration
{
    /// <summary>
    /// Centralized configuration constants to eliminate hardcoded values across the solution
    /// </summary>
    public static class ConfigurationConstants
    {
        // Token Configuration
        public const string TOKEN_SECRET_KEY = "TOKEN_SECRET";
        public const string DEFAULT_TOKEN_SECRET = "dev-secret-change-me";

        // SMTP Configuration
        public const string SMTP_HOST_KEY = "Smtp:Host";
        public const string SMTP_PORT_KEY = "Smtp:Port";
        public const string SMTP_USER_KEY = "Smtp:User";
        public const string SMTP_PASSWORD_KEY = "Smtp:Password";
        public const string SMTP_USE_START_TLS_KEY = "Smtp:UseStartTls";
        public const string SMTP_USE_SSL_KEY = "Smtp:UseSsl";
        
        public const string DEFAULT_SMTP_HOST = "smtp.gmail.com";
        public const int DEFAULT_SMTP_PORT = 587;
        public const bool DEFAULT_USE_START_TLS = true;
        public const bool DEFAULT_USE_SSL = false;

        // WhatsApp Configuration
        public const string WHATSAPP_ACCESS_TOKEN_KEY = "WHATSAPP_ACCESS_TOKEN";
        public const string WHATSAPP_PHONE_NUMBER_ID_KEY = "WHATSAPP_PHONE_NUMBER_ID";
        public const string WHATSAPP_API_VERSION_KEY = "WHATSAPP_API_VERSION";
        public const string WHATSAPP_DEFAULT_TEMPLATE_NAME_KEY = "WHATSAPP__DEFAULTTEMPLATENAME";
        public const string WHATSAPP_DEFAULT_TEMPLATE_LANG_KEY = "WHATSAPP__DEFAULTTEMPLATELANG";
        
        public const string DEFAULT_WHATSAPP_API_VERSION = "v22.0";
        public const string DEFAULT_WHATSAPP_TEMPLATE_NAME = "alert_confirmation";
        public const string DEFAULT_WHATSAPP_TEMPLATE_LANG = "fr";

        // Database Configuration
        public const string CONNECTION_STRING_KEY = "CONNECTIONSTRINGS__DEFAULTCONNECTION";
        public const string DEFAULT_CONNECTION_STRING = "";

        // Base URL Configuration
        public const string BASE_URL_KEY = "BASE_URL";
        public const string DEFAULT_BASE_URL = "http://localhost:5285";

        // Hangfire Configuration
        public const string HANGFIRE_CONNECTION_STRING_KEY = "HANGFIRE_CONNECTION_STRING";
        public const string DEFAULT_HANGFIRE_CONNECTION_STRING = "";

        // Logging Configuration
        public const string LOG_LEVEL_KEY = "Logging:LogLevel:Default";
        public const string DEFAULT_LOG_LEVEL = "Information";

        // Email Template Configuration
        public const string EMAIL_TEMPLATE_PATH_KEY = "EmailTemplate:Path";
        public const string DEFAULT_EMAIL_TEMPLATE_PATH = "Templates/EmailTemplate.html";

        // WebPush Configuration
        public const string WEBPUSH_PUBLIC_KEY_KEY = "WebPush:PublicKey";
        public const string WEBPUSH_PRIVATE_KEY_KEY = "WebPush:PrivateKey";
        public const string WEBPUSH_SUBJECT_KEY = "WebPush:Subject";
        
        public const string DEFAULT_WEBPUSH_SUBJECT = "mailto:admin@alertsystem.com";

        // API Configuration
        public const string API_KEY_HEADER = "X-API-Key";
        public const string API_KEY_CONFIG_KEY = "ApiKey";
        public const string DEFAULT_API_KEY = "your-api-key-here";

        // Worker Configuration
        public const string WORKER_POLLING_INTERVAL_KEY = "Worker:PollingInterval";
        public const int DEFAULT_POLLING_INTERVAL_SECONDS = 30;

        // SignalR Configuration
        public const string SIGNALR_HUB_PATH = "/notifications";
    }
}

