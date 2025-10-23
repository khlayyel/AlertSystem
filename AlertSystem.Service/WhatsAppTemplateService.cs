using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AlertSystem.Services;

namespace AlertSystem.Service
{
    public interface IWhatsAppTemplateService
    {
        Task<bool> SendAlertTemplateAsync(string phoneNumber, string title, string message, string senderName, string confirmationUrl);
    }

    public class WhatsAppTemplateService : IWhatsAppTemplateService
    {
        private readonly IWhatsAppService? _whatsAppService;
        private readonly ILogger<WhatsAppTemplateService> _logger;
        private readonly IConfiguration _configuration;

        public WhatsAppTemplateService(IWhatsAppService whatsAppService, ILogger<WhatsAppTemplateService> logger, IConfiguration configuration)
        {
            _whatsAppService = whatsAppService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> SendAlertTemplateAsync(string phoneNumber, string title, string message, string senderName, string confirmationUrl)
        {
            try
            {
                _logger.LogInformation("Sending WhatsApp template alert to {PhoneNumber}", phoneNumber);

                // Read template configuration from appsettings.json
                var templateName = _configuration["WhatsApp:DefaultTemplateName"] ?? "alert_confirmation";
                var languageCode = _configuration["WhatsApp:DefaultTemplateLang"] ?? "fr";

                _logger.LogInformation("🔧 TEMPLATE SERVICE DEBUG: TemplateName={TemplateName}, LanguageCode={LanguageCode}", 
                    templateName, languageCode);

                // Variables du template selon la structure approuvée par Meta
                // Header {{1}} : Titre de l'alerte
                // Body {{2}} : Description du message  
                // Body {{3}} : Nom de l'expéditeur
                // Body {{4}} : Timestamp
                // Button URL {{5}} : URL de confirmation
                var variables = new Dictionary<string, string>
                {
                    { "1", title },           // Header {{1}} : Titre de l'alerte
                    { "2", message },        // Body {{2}} : Description du message
                    { "3", senderName },     // Body {{3}} : Nom de l'expéditeur
                    { "4", DateTime.Now.ToString("dd/MM/yyyy HH:mm") }, // Body {{4}} : Timestamp
                    { "5", confirmationUrl } // Button URL {{5}} : URL de confirmation
                };

                _logger.LogInformation("🔧 TEMPLATE VARIABLES: {Variables}", 
                    string.Join(", ", variables.Select(kv => $"{kv.Key}={kv.Value}")));

                // CRITICAL: Log the exact confirmation URL being sent
                _logger.LogInformation("🔧 CONFIRMATION URL DEBUG: PhoneNumber={PhoneNumber}, ConfirmationUrl={ConfirmationUrl}", 
                    phoneNumber, confirmationUrl);

                var success = await _whatsAppService.SendTemplateAsync(phoneNumber, templateName, languageCode, variables);
                
                if (success)
                {
                    _logger.LogInformation("✅ WhatsApp template sent successfully to {PhoneNumber}", phoneNumber);
                }
                else
                {
                    _logger.LogWarning("❌ Failed to send WhatsApp template to {PhoneNumber}", phoneNumber);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending WhatsApp template to {PhoneNumber}", phoneNumber);
                return false;
            }
        }
    }
}
