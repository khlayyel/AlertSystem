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

                // Try free-form message first
                var success = await TryFreeFormMessage(cleanPhoneNumber, message);
                if (success)
                {
                    return true;
                }

                // If free-form fails, try template message
                _logger.LogWarning("Free-form message failed, trying template message for {PhoneNumber}", cleanPhoneNumber);
                return await TryTemplateMessage(cleanPhoneNumber, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception sending WhatsApp message to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        private async Task<bool> TryFreeFormMessage(string phoneNumber, string message)
        {
            try
            {
                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "text",
                    text = new { body = message }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("=== WHATSAPP FREE-FORM SEND === To: {PhoneNumber}, URL: {URL}", 
                    phoneNumber, $"{_httpClient.BaseAddress}{_phoneNumberId}/messages");
                _logger.LogInformation("WA payload(text): {Payload}", json);

                var response = await _httpClient.PostAsync($"{_phoneNumberId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("WhatsApp Free-Form Response - Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ WhatsApp free-form message sent successfully to {PhoneNumber}", phoneNumber);
                    return true;
                }
                else
                {
                    // Parse error details
                    await LogWhatsAppError(phoneNumber, response.StatusCode, responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in TryFreeFormMessage for {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        private async Task<bool> TryTemplateMessage(string phoneNumber, string message)
        {
            try
            {
                // Use hello_world template as fallback
                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "template",
                    template = new 
                    { 
                        name = "hello_world", 
                        language = new { code = "en_US" } 
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("=== WHATSAPP TEMPLATE SEND === To: {PhoneNumber}, Template: hello_world", phoneNumber);
                _logger.LogInformation("WA payload(template): {Payload}", json);

                var response = await _httpClient.PostAsync($"{_phoneNumberId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("WhatsApp Template Response - Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ WhatsApp template message sent successfully to {PhoneNumber}", phoneNumber);
                    return true;
                }
                else
                {
                    await LogWhatsAppError(phoneNumber, response.StatusCode, responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in TryTemplateMessage for {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        private async Task LogWhatsAppError(string phoneNumber, System.Net.HttpStatusCode statusCode, string responseContent)
        {
            try
            {
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;
                
                if (root.TryGetProperty("error", out var errorElement))
                {
                    var errorCode = errorElement.TryGetProperty("code", out var codeElement) ? codeElement.GetInt32() : 0;
                    var errorMessage = errorElement.TryGetProperty("message", out var msgElement) ? msgElement.GetString() : "Unknown error";
                    var errorType = errorElement.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : "Unknown type";
                    
                    _logger.LogError("❌ WhatsApp API Error for {PhoneNumber} - Code: {ErrorCode}, Type: {ErrorType}, Message: {ErrorMessage}", 
                        phoneNumber, errorCode, errorType, errorMessage);
                        
                    // Log specific error explanations
                    switch (errorCode)
                    {
                        case 131026:
                            _logger.LogWarning("🔒 Error 131026: Message undeliverable - User not opted in or 24-hour window expired for {PhoneNumber}", phoneNumber);
                            break;
                        case 131047:
                            _logger.LogWarning("📵 Error 131047: Re-engagement message - User needs to message business first for {PhoneNumber}", phoneNumber);
                            break;
                        case 131051:
                            _logger.LogWarning("⏰ Error 131051: Unsupported message type or template required for {PhoneNumber}", phoneNumber);
                            break;
                        default:
                            _logger.LogWarning("❓ Unknown WhatsApp error code {ErrorCode} for {PhoneNumber}", errorCode, phoneNumber);
                            break;
                    }
                }
                else
                {
                    _logger.LogError("❌ WhatsApp API failed for {PhoneNumber} - Status: {StatusCode}, Raw Response: {Response}", 
                        phoneNumber, statusCode, responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing WhatsApp API error response for {PhoneNumber}", phoneNumber);
                _logger.LogError("Raw response was: {Response}", responseContent);
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