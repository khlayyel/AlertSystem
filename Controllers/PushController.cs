using System.Security.Claims;
using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Controllers
{
    [Authorize]
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (string.IsNullOrWhiteSpace(dto.Endpoint) || string.IsNullOrWhiteSpace(dto.P256dh) || string.IsNullOrWhiteSpace(dto.Auth)) return BadRequest();
            var existing = await _db.WebPushSubscriptions.FirstOrDefaultAsync(x => x.UserId == userId && x.Endpoint == dto.Endpoint);
            if (existing == null)
            {
                _db.WebPushSubscriptions.Add(new WebPushSubscription{ UserId = userId, Endpoint = dto.Endpoint, P256dh = dto.P256dh, Auth = dto.Auth, CreatedAt = DateTime.UtcNow });
            }
            else
            {
                existing.P256dh = dto.P256dh; existing.Auth = dto.Auth;
            }
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Unsubscribe([FromBody] SubscribeDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var row = await _db.WebPushSubscriptions.FirstOrDefaultAsync(x => x.UserId == userId && x.Endpoint == dto.Endpoint);
            if (row != null){ _db.WebPushSubscriptions.Remove(row); await _db.SaveChangesAsync(); }
            return Ok();
        }
    }
}


