using Microsoft.AspNetCore.Mvc;
using AlertSystem.Services;
using System.Text.Json;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public sealed class WebPushController : ControllerBase
    {
        private readonly IWebPushService _webPushService;
        private readonly ILogger<WebPushController> _logger;
        private readonly IConfiguration _config;

        public WebPushController(IWebPushService webPushService, ILogger<WebPushController> logger, IConfiguration config)
        {
            _webPushService = webPushService;
            _logger = logger;
            _config = config;
        }

        [HttpGet("vapid-public-key")]
        public IActionResult GetVapidPublicKey()
        {
            try
            {
                var publicKey = _config["WebPush:PublicKey"] ?? "BEl62iUYgUivxIkv69yViEuiBIa40HI2up27681NNApoeymTRrBT6sNdRcMwZDFkF3xF0vckFiJ7_tqsRWTxaX8";
                
                return Ok(new { publicKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting VAPID public key");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            try
            {
                if (request.UserId <= 0 || string.IsNullOrWhiteSpace(request.Endpoint))
                {
                    return BadRequest(new { error = "UserId and Endpoint are required" });
                }

                var deviceToken = await _webPushService.SubscribeUserAsync(
                    request.UserId,
                    request.Endpoint,
                    request.P256dh,
                    request.Auth
                );

                _logger.LogInformation("User {UserId} subscribed to web push notifications", request.UserId);

                return Ok(new { 
                    success = true, 
                    deviceToken,
                    message = "Successfully subscribed to notifications" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing user {UserId} to web push", request.UserId);
                return StatusCode(500, new { error = "Failed to subscribe to notifications" });
            }
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
        {
            try
            {
                if (request.UserId <= 0 || string.IsNullOrWhiteSpace(request.Endpoint))
                {
                    return BadRequest(new { error = "UserId and Endpoint are required" });
                }

                var success = await _webPushService.UnsubscribeUserAsync(request.UserId, request.Endpoint);

                if (success)
                {
                    _logger.LogInformation("User {UserId} unsubscribed from web push notifications", request.UserId);
                    return Ok(new { success = true, message = "Successfully unsubscribed from notifications" });
                }
                else
                {
                    return NotFound(new { error = "Subscription not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing user {UserId} from web push", request.UserId);
                return StatusCode(500, new { error = "Failed to unsubscribe from notifications" });
            }
        }

        [HttpPost("test-notification")]
        public async Task<IActionResult> TestNotification([FromBody] TestNotificationRequest request)
        {
            try
            {
                if (request.UserId <= 0)
                {
                    return BadRequest(new { error = "UserId is required" });
                }

                var deviceTokens = await _webPushService.GetUserDeviceTokensAsync(request.UserId);
                
                if (!deviceTokens.Any())
                {
                    return NotFound(new { error = "No device tokens found for user" });
                }

                var results = new List<object>();
                
                foreach (var token in deviceTokens)
                {
                    var success = await _webPushService.SendNotificationAsync(
                        token,
                        request.Title ?? "Test Notification",
                        request.Message ?? "This is a test notification from AlertSystem"
                    );

                    results.Add(new { deviceToken = token, success });
                }

                _logger.LogInformation("Sent test notification to user {UserId} on {Count} devices", 
                    request.UserId, deviceTokens.Count);

                return Ok(new { 
                    success = true, 
                    message = $"Test notification sent to {deviceTokens.Count} device(s)",
                    results 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test notification to user {UserId}", request.UserId);
                return StatusCode(500, new { error = "Failed to send test notification" });
            }
        }

        [HttpGet("user/{userId}/devices")]
        public async Task<IActionResult> GetUserDevices(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { error = "Valid UserId is required" });
                }

                var deviceTokens = await _webPushService.GetUserDeviceTokensAsync(userId);

                return Ok(new { 
                    userId,
                    deviceCount = deviceTokens.Count,
                    devices = deviceTokens.Select(token => new { 
                        deviceToken = token,
                        shortToken = token.Length > 50 ? token.Substring(0, 50) + "..." : token
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting devices for user {UserId}", userId);
                return StatusCode(500, new { error = "Failed to get user devices" });
            }
        }
    }

    public sealed class SubscribeRequest
    {
        public int UserId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    public sealed class UnsubscribeRequest
    {
        public int UserId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
    }

    public sealed class TestNotificationRequest
    {
        public int UserId { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
    }
}
