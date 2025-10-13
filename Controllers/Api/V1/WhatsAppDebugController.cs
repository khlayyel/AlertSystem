using Microsoft.AspNetCore.Mvc;
using AlertSystem.Services;
using System.Text.Json;
using System.Text;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/whatsapp-debug")]
    public class WhatsAppDebugController : ControllerBase
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<WhatsAppDebugController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public WhatsAppDebugController(
            IWhatsAppService whatsAppService, 
            ILogger<WhatsAppDebugController> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _whatsAppService = whatsAppService;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            var config = new
            {
                HasAccessToken = !string.IsNullOrEmpty(_configuration["WhatsApp:AccessToken"]),
                PhoneNumberId = _configuration["WhatsApp:PhoneNumberId"],
                ApiVersion = _configuration["WhatsApp:ApiVersion"] ?? "v20.0",
                BaseUrl = $"https://graph.facebook.com/{_configuration["WhatsApp:ApiVersion"] ?? "v20.0"}/",
                Timestamp = DateTime.UtcNow
            };
            
            return Ok(config);
        }

        [HttpPost("test-free-form")]
        public async Task<IActionResult> TestFreeForm([FromBody] WhatsAppDebugRequest request)
        {
            try
            {
                _logger.LogInformation("Testing free-form WhatsApp message to {PhoneNumber}", request.PhoneNumber);
                
                var success = await _whatsAppService.SendMessageAsync(request.PhoneNumber, request.Message);
                
                return Ok(new
                {
                    Success = success,
                    Message = success ? "Free-form message sent successfully" : "Free-form message failed",
                    PhoneNumber = request.PhoneNumber,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing free-form WhatsApp message");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("test-template")]
        public async Task<IActionResult> TestTemplate([FromBody] WhatsAppDebugRequest request)
        {
            try
            {
                _logger.LogInformation("Testing template WhatsApp message to {PhoneNumber}", request.PhoneNumber);
                
                var success = await _whatsAppService.SendTemplateHelloAsync(request.PhoneNumber);
                
                return Ok(new
                {
                    Success = success,
                    Message = success ? "Template message sent successfully" : "Template message failed",
                    PhoneNumber = request.PhoneNumber,
                    Template = "hello_world",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing template WhatsApp message");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("test-direct-api")]
        public async Task<IActionResult> TestDirectApi([FromBody] WhatsAppDebugRequest request)
        {
            try
            {
                var accessToken = _configuration["WhatsApp:AccessToken"];
                var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
                var apiVersion = _configuration["WhatsApp:ApiVersion"] ?? "v20.0";

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(phoneNumberId))
                {
                    return BadRequest(new { Error = "WhatsApp configuration missing" });
                }

                var cleanPhoneNumber = request.PhoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "");

                // Test free-form message first
                var freeFormResult = await TestDirectFreeForm(accessToken, phoneNumberId, apiVersion, cleanPhoneNumber, request.Message);
                
                if (freeFormResult.Success)
                {
                    return Ok(freeFormResult);
                }

                // If free-form fails, test template
                var templateResult = await TestDirectTemplate(accessToken, phoneNumberId, apiVersion, cleanPhoneNumber);
                
                return Ok(new
                {
                    FreeFormResult = freeFormResult,
                    TemplateResult = templateResult,
                    Recommendation = GetRecommendation(freeFormResult, templateResult)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct API test");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        private async Task<TestResult> TestDirectFreeForm(string accessToken, string phoneNumberId, string apiVersion, string phoneNumber, string message)
        {
            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri($"https://graph.facebook.com/{apiVersion}/");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "text",
                    text = new { body = message }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{phoneNumberId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return new TestResult
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Response = responseContent,
                    Type = "FreeForm",
                    ErrorDetails = response.IsSuccessStatusCode ? null : await ParseWhatsAppError(responseContent)
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    Success = false,
                    StatusCode = 0,
                    Response = ex.Message,
                    Type = "FreeForm",
                    ErrorDetails = new { Exception = ex.Message }
                };
            }
        }

        private async Task<TestResult> TestDirectTemplate(string accessToken, string phoneNumberId, string apiVersion, string phoneNumber)
        {
            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri($"https://graph.facebook.com/{apiVersion}/");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

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

                var response = await client.PostAsync($"{phoneNumberId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return new TestResult
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Response = responseContent,
                    Type = "Template",
                    ErrorDetails = response.IsSuccessStatusCode ? null : await ParseWhatsAppError(responseContent)
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    Success = false,
                    StatusCode = 0,
                    Response = ex.Message,
                    Type = "Template",
                    ErrorDetails = new { Exception = ex.Message }
                };
            }
        }

        private async Task<object?> ParseWhatsAppError(string responseContent)
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
                    
                    return new
                    {
                        Code = errorCode,
                        Message = errorMessage,
                        Type = errorType,
                        Explanation = GetErrorExplanation(errorCode)
                    };
                }
                
                return null;
            }
            catch
            {
                return new { RawResponse = responseContent };
            }
        }

        private string GetErrorExplanation(int errorCode)
        {
            return errorCode switch
            {
                131026 => "Message undeliverable - User not opted in or 24-hour window expired. User needs to message your business first.",
                131047 => "Re-engagement message required - User needs to message business first to open messaging window.",
                131051 => "Unsupported message type or template required - Free-form messages not allowed, use approved templates.",
                100 => "Invalid parameter - Check phone number format and message content.",
                190 => "Invalid access token - Check your WhatsApp Business API token.",
                _ => $"Unknown error code {errorCode} - Check WhatsApp Business API documentation."
            };
        }

        private object GetRecommendation(TestResult freeForm, TestResult template)
        {
            if (freeForm.Success)
            {
                return new { Message = "✅ Free-form messaging works! Your setup is correct." };
            }

            if (template.Success)
            {
                return new 
                { 
                    Message = "⚠️ Only template messaging works. User needs to message your business first for free-form messages.",
                    Solution = "Ask the user to send any message to your WhatsApp Business number, then try again within 24 hours."
                };
            }

            return new 
            { 
                Message = "❌ Both free-form and template messaging failed. Check your WhatsApp Business configuration.",
                Solutions = new[]
                {
                    "Verify your WhatsApp Business API access token",
                    "Check if your phone number ID is correct",
                    "Ensure your WhatsApp Business account is verified",
                    "Check if the recipient number is valid"
                }
            };
        }

        [HttpGet("error-codes")]
        public IActionResult GetErrorCodes()
        {
            var errorCodes = new Dictionary<int, object>
            {
                { 131026, new { Description = "Message undeliverable", Solution = "User must message business first or use templates" } },
                { 131047, new { Description = "Re-engagement required", Solution = "User must initiate conversation" } },
                { 131051, new { Description = "Template required", Solution = "Use approved message templates" } },
                { 100, new { Description = "Invalid parameter", Solution = "Check phone number format and message content" } },
                { 190, new { Description = "Invalid access token", Solution = "Verify WhatsApp Business API token" } }
            };

            return Ok(errorCodes);
        }
    }

    public class WhatsAppDebugRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class TestResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Response { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public object? ErrorDetails { get; set; }
    }
}
