using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Worker.Services
{
    public class WhatsAppSender : IWhatsAppSender
    {
        private readonly ILogger<WhatsAppSender> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _accessToken;
        private readonly string? _phoneNumberId;
        private readonly string _apiVersion;

        public WhatsAppSender(ILogger<WhatsAppSender> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _accessToken = configuration["WhatsApp:AccessToken"];
            _phoneNumberId = configuration["WhatsApp:PhoneNumberId"];
            _apiVersion = configuration["WhatsApp:ApiVersion"] ?? "v22.0";
        }

        public async Task SendAsync(string phoneNumber, string title, string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_accessToken) || string.IsNullOrWhiteSpace(_phoneNumberId))
            {
                _logger.LogWarning("WhatsApp credentials missing, skipping send");
                return;
            }

            try
            {
                var normalizedPhone = NormalizePhoneNumber(phoneNumber);
                var digitsOnly = Regex.Replace(normalizedPhone, "[^0-9]", "");

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var url = $"https://graph.facebook.com/{_apiVersion}/{_phoneNumberId}/messages";

                // Try with E.164 format first
                var success = await TrySendMessage(httpClient, url, normalizedPhone, title, message, cancellationToken);
                
                if (!success)
                {
                    // Try with digits only
                    success = await TrySendMessage(httpClient, url, digitsOnly, title, message, cancellationToken);
                }

                if (!success)
                {
                    // Try template as last resort
                    success = await TrySendTemplate(httpClient, url, normalizedPhone, cancellationToken);
                    if (!success)
                    {
                        await TrySendTemplate(httpClient, url, digitsOnly, cancellationToken);
                    }
                }

                if (success)
                {
                    _logger.LogInformation("WhatsApp message sent to {Phone}", phoneNumber);
                }
                else
                {
                    _logger.LogWarning("Failed to send WhatsApp message to {Phone}", phoneNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp to {Phone}", phoneNumber);
            }
        }

        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;
            
            var cleaned = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
            
            if (cleaned.StartsWith("+")) return cleaned;
            if (cleaned.StartsWith("0") && cleaned.Length >= 9) return "+216" + cleaned.Substring(1);
            if (cleaned.StartsWith("216") && cleaned.Length == 11) return "+" + cleaned;
            if (cleaned.Length == 8) return "+216" + cleaned;
            
            return "+" + cleaned;
        }

        private async Task<bool> TrySendMessage(HttpClient httpClient, string url, string phoneNumber, string title, string message, CancellationToken cancellationToken)
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                to = phoneNumber,
                type = "text",
                text = new { body = $"{title}\n\n{message}" }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("WhatsApp API error for {Phone}: {Error}", phoneNumber, error);
            }

            return response.IsSuccessStatusCode;
        }

        private async Task<bool> TrySendTemplate(HttpClient httpClient, string url, string phoneNumber, CancellationToken cancellationToken)
        {
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

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
    }
}
