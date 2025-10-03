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
                _logger.LogInformation("WA cfg check: base={Base}, phoneId={PhoneId}, tokenLen={Len}, tokenTail={Tail}",
                    _httpClient.BaseAddress, _phoneNumberId, _accessToken.Length, _accessToken[^6..]);

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
                _logger.LogInformation("WA payload(text): {Payload}", json);

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
                _logger.LogInformation("SendAlertAsync -> will send text to {Phone}", phoneNumber);
                return await SendMessageAsync(phoneNumber, formattedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp alert to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendTemplateHelloAsync(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_accessToken) || string.IsNullOrWhiteSpace(_phoneNumberId))
                {
                    _logger.LogError("WhatsApp configuration missing - cannot send template");
                    return false;
                }

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
                    type = "template",
                    template = new { name = "hello_world", language = new { code = "en_US" } }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                _logger.LogInformation("=== WHATSAPP TEMPLATE SEND === To: {PhoneNumber}, URL: {URL}", cleanPhoneNumber, $"{_httpClient.BaseAddress}{_phoneNumberId}/messages");
                _logger.LogInformation("WA payload(template): {Payload}", json);
                var response = await _httpClient.PostAsync($"{_phoneNumberId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("WhatsApp API Response - Status: {StatusCode}, Content: {Content}", response.StatusCode, responseContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending WhatsApp template to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        private string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return phoneNumber;

            // Remove all non-digits except '+'
            var cleaned = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());

            // If already in E.164 format
            if (cleaned.StartsWith("+"))
                return cleaned;

            // Handle numbers starting with local trunk '0' → replace with +216 (Tunisia)
            if (cleaned.StartsWith("0") && cleaned.Length >= 9)
            {
                return "+216" + cleaned.Substring(1);
            }

            // If already starts with country code without '+', convert to E.164 by prefixing '+'
            // Tunisia: country code 216 → length should be 11 (216 + 8 digits)
            if (cleaned.StartsWith("216") && cleaned.Length == 11)
            {
                return "+" + cleaned;
            }

            // If 8-digit local number, assume Tunisia and add +216
            if (cleaned.Length == 8)
            {
                return "+216" + cleaned;
            }

            // Fallback: prefix '+' to whatever we have
            return "+" + cleaned;
        }
    }
}