using System.Text;
using System.Text.Json;

namespace AlertSystem.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly string _accessToken;
        private readonly string _phoneNumberId;

        public WhatsAppService(HttpClient httpClient, ILogger<WhatsAppService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // Configuration depuis appsettings.json
            _accessToken = configuration["WhatsApp:AccessToken"] ?? "";
            _phoneNumberId = configuration["WhatsApp:PhoneNumberId"] ?? "";
            var apiVersion = configuration["WhatsApp:ApiVersion"] ?? "v20.0";
            
            if (string.IsNullOrWhiteSpace(_accessToken) || string.IsNullOrWhiteSpace(_phoneNumberId))
            {
                _logger.LogWarning("WhatsApp configuration incomplete - AccessToken or PhoneNumberId missing");
            }
            
            // Configuration de base pour l'API WhatsApp Business
            _httpClient.BaseAddress = new Uri($"https://graph.facebook.com/{apiVersion}/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            
            _logger.LogInformation("WhatsApp service initialized with PhoneNumberId: {PhoneNumberId}, API: {ApiVersion}", 
                _phoneNumberId, apiVersion);
        }

        public async Task<bool> SendMessageAsync(string phoneNumber, string message)
        {
            try
            {
                // Validation des paramètres de configuration
                if (string.IsNullOrWhiteSpace(_accessToken) || string.IsNullOrWhiteSpace(_phoneNumberId))
                {
                    _logger.LogError("WhatsApp configuration missing - cannot send message");
                    return false;
                }

                // Nettoyer le numéro de téléphone (enlever espaces, tirets, etc.)
                var cleanPhoneNumber = CleanPhoneNumber(phoneNumber);
                
                if (string.IsNullOrWhiteSpace(cleanPhoneNumber))
                {
                    _logger.LogError("Invalid phone number: {PhoneNumber}", phoneNumber);
                    return false;
                }

                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = cleanPhoneNumber,
                    type = "text",
                    text = new { body = message }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("=== WHATSAPP SEND === To: {PhoneNumber}, URL: {URL}", 
                    cleanPhoneNumber, $"{_httpClient.BaseAddress}{_phoneNumberId}/messages");
                _logger.LogDebug("WhatsApp payload: {Payload}", json);

                var response = await _httpClient.PostAsync($"{_phoneNumberId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("WhatsApp API Response - Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ WhatsApp message sent successfully to {PhoneNumber}", cleanPhoneNumber);
                    return true;
                }
                else
                {
                    _logger.LogError("❌ Failed to send WhatsApp message to {PhoneNumber}. Status: {StatusCode}, Response: {Response}", 
                        cleanPhoneNumber, response.StatusCode, responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception sending WhatsApp message to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendAlertAsync(string phoneNumber, string title, string message, string senderName)
        {
            try
            {
                // Format du message pour les alertes
                var formattedMessage = $"🚨 *{title}*\n\n" +
                                     $"{message}\n\n" +
                                     $"📤 Envoyé par: {senderName}\n" +
                                     $"⏰ {DateTime.Now:dd/MM/yyyy HH:mm}";

                return await SendMessageAsync(phoneNumber, formattedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp alert to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        private string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return phoneNumber;

            // Enlever tous les caractères non numériques sauf le +
            var cleaned = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());
            
            // Si le numéro commence par +, le garder tel quel
            if (cleaned.StartsWith("+"))
                return cleaned;
            
            // Si le numéro commence par 0, remplacer par le code pays (exemple: Tunisie +216)
            if (cleaned.StartsWith("0"))
            {
                // Remplacer le 0 initial par +216 (code Tunisie)
                // Vous pouvez modifier selon votre pays
                return "+216" + cleaned.Substring(1);
            }
            
            // Si pas de code pays, ajouter +216 par défaut
            if (!cleaned.StartsWith("+") && cleaned.Length >= 8)
            {
                return "+216" + cleaned;
            }
            
            return cleaned;
        }
    }
}