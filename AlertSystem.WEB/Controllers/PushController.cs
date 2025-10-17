using System.Security.Claims;
using AlertSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.WEB.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public sealed class PushController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        private readonly IWebPushService _webPush;
        public PushController(IConfiguration cfg, IWebPushService webPush){ _cfg = cfg; _webPush = webPush; }

        [HttpGet]
        public IActionResult PublicKey()
        {
            var key = _cfg["WebPush:PublicKey"] ?? string.Empty;
            return Ok(new { publicKey = key });
        }

        public sealed class SubscribeDto{ public string Endpoint { get; set; } = string.Empty; public string P256dh { get; set; } = string.Empty; public string Auth { get; set; } = string.Empty; }

        [HttpPost]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeDto dto)
        {
            return BadRequest(new { error = "Use /api/v1/webpush/subscribe instead" });
        }

        [HttpPost]
        public async Task<IActionResult> Unsubscribe([FromBody] SubscribeDto dto)
        {
            return BadRequest(new { error = "Use /api/v1/webpush/unsubscribe instead" });
        }
    }
}


