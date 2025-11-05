using System.Security.Claims;
using AlertSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.WEB.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]/[action]")]
    public sealed class PushController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        private readonly IWebPushService _webPush;
        public PushController(IConfiguration cfg, IWebPushService webPush){ _cfg = cfg; _webPush = webPush; }

        [HttpGet]
        public IActionResult VapidPublicKey()
        {
            var key = _cfg["WebPush:PublicKey"] ?? string.Empty;
            return Ok(new { publicKey = key });
        }

        public sealed class SubscribeDto{ public string Endpoint { get; set; } = string.Empty; public string P256dh { get; set; } = string.Empty; public string Auth { get; set; } = string.Empty; }

        [HttpPost]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Endpoint) || string.IsNullOrWhiteSpace(dto.P256dh) || string.IsNullOrWhiteSpace(dto.Auth))
            {
                return BadRequest(new { error = "Invalid subscription payload" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var endpoint = await _webPush.SubscribeUserAsync(userId, dto.Endpoint, dto.P256dh, dto.Auth);
            Console.WriteLine($"[WebPush] Subscribe OK user={userId} endpoint={(endpoint?.Length>50?endpoint[..50]+"...":endpoint)}");
            return Ok(new { success = true, endpoint });
        }

        [HttpPost]
        public async Task<IActionResult> Unsubscribe([FromBody] SubscribeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Endpoint))
            {
                return BadRequest(new { error = "Endpoint required" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var ok = await _webPush.UnsubscribeUserAsync(userId, dto.Endpoint);
            return Ok(new { success = ok });
        }
    }
}


