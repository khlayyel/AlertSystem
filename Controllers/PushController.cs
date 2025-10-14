using System.Security.Claims;
using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public sealed class PushController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _cfg;
        public PushController(ApplicationDbContext db, IConfiguration cfg){ _db = db; _cfg = cfg; }

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
            // Note: Ce contrôleur est obsolète, utilisez /api/v1/webpush/subscribe à la place
            return BadRequest(new { error = "Use /api/v1/webpush/subscribe instead" });
        }

        [HttpPost]
        public async Task<IActionResult> Unsubscribe([FromBody] SubscribeDto dto)
        {
            // Note: Ce contrôleur est obsolète, utilisez /api/v1/webpush/unsubscribe à la place
            return BadRequest(new { error = "Use /api/v1/webpush/unsubscribe instead" });
        }
    }
}


