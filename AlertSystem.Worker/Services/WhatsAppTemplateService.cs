using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AlertSystem.Worker.Services;

namespace AlertSystem.Worker.Services
{
    public class WhatsAppTemplateService : AlertSystem.Service.IWhatsAppTemplateService
    {
        private readonly IWhatsAppSender _whatsAppSender;
        private readonly ILogger<WhatsAppTemplateService> _logger;
        private readonly IConfiguration _configuration;

        public WhatsAppTemplateService(IWhatsAppSender whatsAppSender, ILogger<WhatsAppTemplateService> logger, IConfiguration configuration)
        {
            _whatsAppSender = whatsAppSender;
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

                _logger.LogInformation("🔧 WORKER TEMPLATE DEBUG: TemplateName={TemplateName}, LanguageCode={LanguageCode}", 
                    templateName, languageCode);

                // For now, we'll send as a formatted message since IWhatsAppSender doesn't have template support
                // TODO: Extend IWhatsAppSender to support templates or use IWhatsAppService directly
                var templateMessage = $"🚨 {title}\n\n{message}\n\nEnvoyé par : {senderName}\n{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}\n\nConfirmer : {confirmationUrl}";
                
                await _whatsAppSender.SendAsync(phoneNumber, title, templateMessage, CancellationToken.None);

                _logger.LogInformation("✅ WhatsApp template sent successfully to {PhoneNumber}", phoneNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending WhatsApp template to {PhoneNumber}", phoneNumber);
                return false;
            }
        }
    }
}
