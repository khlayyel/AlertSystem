using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace AlertSystem.WEB.Controllers
{
    [AllowAnonymous]
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
            if (string.IsNullOrWhiteSpace(t)) 
            {
                return View("Error", new { Message = "Token manquant", Title = "Erreur de confirmation" });
            }
            
            if (!_tokens.TryValidate(t, out var payload)) 
            {
                return View("Error", new { Message = "Token invalide ou expiré", Title = "Erreur de confirmation" });
            }

            var alerte = await _db.Alerte
                .Include(a => a.TypeEnvoie)
                .FirstOrDefaultAsync(a => a.AlertRecordId == payload.AlerteId);
                
            if (alerte == null) 
            {
                return View("Error", new { Message = "Alerte introuvable", Title = "Erreur de confirmation" });
            }

            // Résoudre le groupe et marquer toutes les lignes de ce groupe comme lues
            var baseRow = await _db.Alerte
                .Where(a => a.AlertRecordId == payload.AlerteId)
                .Select(a => new { a.AlertGroupId })
                .FirstOrDefaultAsync();

            if (baseRow == null)
            {
                return View("Error", new { Message = "Alerte introuvable (groupe)", Title = "Erreur de confirmation" });
            }

            var rows = await _db.Alerte
                .Where(a => a.AlertGroupId == baseRow.AlertGroupId)
                .ToListAsync();

            var confirmedCount = 0;
            foreach (var h in rows)
            {
                if (h.EtatId != 2 && h.EtatId != 4) // Only update if not already confirmed
                {
                    // Map: 3 (Obligatoire Non Confirmée) -> 4 (Obligatoire Confirmée)
                    // Otherwise: 1 (Non Lu) -> 2 (Lu)
                    h.EtatId = (h.EtatId == 3) ? 4 : 2;
                    h.DateLecture = DateTime.UtcNow;
                    confirmedCount++;
                }
            }

            await _db.SaveChangesAsync();

            // Broadcast real-time update
            try
            {
                var hub = HttpContext.RequestServices.GetService<IHubContext<AlertSystem.Infrastructure.Hubs.NotificationHub>>();
                if (hub != null)
                {
                    await hub.Clients.All.SendAsync("ReceiveNotification", "UpdateKpis", null);
                    await hub.Clients.All.SendAsync("ReceiveNotification", "AlertStatusUpdated", new { groupId = baseRow.AlertGroupId, status = "Lu", readAt = DateTime.UtcNow });
                }
            }
            catch { }

            // Return success view with alert details
            return View("Success", new { 
                Alert = alerte, 
                ConfirmedCount = confirmedCount,
                ConfirmationTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            });
        }
    }
}


