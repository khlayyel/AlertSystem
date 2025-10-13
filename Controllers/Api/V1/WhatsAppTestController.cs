using Microsoft.AspNetCore.Mvc;
using AlertSystem.Services;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public sealed class WhatsAppTestController : ControllerBase
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<WhatsAppTestController> _logger;

        public WhatsAppTestController(IWhatsAppService whatsAppService, ILogger<WhatsAppTestController> logger)
        {
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> TestSend([FromBody] WhatsAppTestRequest request)
        {
            try
            {
                _logger.LogInformation("Testing WhatsApp send to: {PhoneNumber}", request.PhoneNumber);
                
                var result = await _whatsAppService.SendMessageAsync(request.PhoneNumber, request.Message);
                
                if (result)
                {
                    _logger.LogInformation("WhatsApp message sent successfully to: {PhoneNumber}", request.PhoneNumber);
                    return Ok(new { 
                        success = true, 
                        message = "WhatsApp message sent successfully",
                        phoneNumber = request.PhoneNumber,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning("WhatsApp message failed to send to: {PhoneNumber}", request.PhoneNumber);
                    return BadRequest(new { 
                        success = false, 
                        message = "Failed to send WhatsApp message",
                        phoneNumber = request.PhoneNumber
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing WhatsApp send to: {PhoneNumber}", request.PhoneNumber);
                return StatusCode(500, new { 
                    success = false, 
                    message = "WhatsApp test failed", 
                    error = ex.Message,
                    phoneNumber = request.PhoneNumber
                });
            }
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                // Don't expose the full token for security
                var config = new
                {
                    hasAccessToken = !string.IsNullOrEmpty(_whatsAppService.GetType().GetField("_accessToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_whatsAppService)?.ToString()),
                    phoneNumberId = "807577429105749",
                    apiVersion = "v22.0",
                    baseUrl = "https://graph.facebook.com/v22.0/",
                    timestamp = DateTime.UtcNow
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting WhatsApp config");
                return StatusCode(500, new { error = "Failed to get WhatsApp config", details = ex.Message });
            }
        }
    }

    public class WhatsAppTestRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = "Test message from AlertSystem";
    }
}
