using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Services;
using Microsoft.Extensions.Configuration;

namespace AlertSystem.WEB.Controllers
{
    [Authorize]
    [ApiController]
    [Route("webpush")]
    public sealed class WebPushController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        private readonly IWebPushService _webPush;

        public WebPushController(IConfiguration cfg, IWebPushService webPush)
        {
            _cfg = cfg; _webPush = webPush;
        }

        [HttpGet("vapid")]
        public IActionResult VapidPublicKey()
        {
            var key = _cfg["WebPush:PublicKey"] ?? string.Empty;
            return Ok(new { publicKey = key });
        }

        // Backward-compat absolute routes expected by frontend JS
        [HttpGet("/Push/VapidPublicKey")]
        public IActionResult VapidPublicKeyCompat() => VapidPublicKey();

        public sealed class SubscribeDto { public string Endpoint { get; set; } = string.Empty; public string P256dh { get; set; } = string.Empty; public string Auth { get; set; } = string.Empty; }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Endpoint) || string.IsNullOrWhiteSpace(dto.P256dh) || string.IsNullOrWhiteSpace(dto.Auth))
                return BadRequest("Invalid subscription payload");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
                return Unauthorized("User not authenticated");

            var endpoint = await _webPush.SubscribeUserAsync(userId, dto.Endpoint, dto.P256dh, dto.Auth);
            return Ok(new { success = true, endpoint });
        }

        // Backward-compat absolute routes
        [HttpPost("/Push/Subscribe")]
        public Task<IActionResult> SubscribeCompat([FromBody] SubscribeDto dto) => Subscribe(dto);

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] SubscribeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Endpoint)) return BadRequest("Endpoint required");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
                return Unauthorized("User not authenticated");

            var ok = await _webPush.UnsubscribeUserAsync(userId, dto.Endpoint);
            return Ok(new { success = ok });
        }
    }
}


