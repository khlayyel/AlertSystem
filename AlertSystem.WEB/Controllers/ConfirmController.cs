using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.WEB.Controllers
{
    [Route("confirm")]
    public sealed class ConfirmController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ConfirmationTokenService _tokens;

        public ConfirmController(ApplicationDbContext db, IConfiguration cfg)
        {
            _db = db;
            _tokens = new ConfirmationTokenService(cfg["TOKEN_SECRET"] ?? "dev-secret-change-me");
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return BadRequest("token missing");
            if (!_tokens.TryValidate(t, out var payload)) return BadRequest("token invalid");

            var alerte = await _db.Alerte.FirstOrDefaultAsync(a => a.AlerteId == payload.AlerteId);
            if (alerte == null) return NotFound("alert not found");

            // Mark as Lu for matching destinataires
            var rows = await _db.HistoriqueAlertes
                .Where(h => h.AlerteId == payload.AlerteId && (
                    (payload.Kind == "wa" && h.DestinatairePhoneNumber == payload.Value) ||
                    (payload.Kind == "email" && h.DestinataireEmail == payload.Value)))
                .ToListAsync();

            foreach (var h in rows)
            {
                h.EtatAlerteId = 2; // Lu
                h.DateLecture = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return Content("La notification a été confirmée", "text/plain", System.Text.Encoding.UTF8);
        }
    }
}


