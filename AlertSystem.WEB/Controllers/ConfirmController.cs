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
                .Include(a => a.AlertType)
                .FirstOrDefaultAsync(a => a.AlertRecordId == payload.AlerteId);
                
            if (alerte == null) 
            {
                return View("Error", new { Message = "Alerte introuvable", Title = "Erreur de confirmation" });
            }

            // Mark as Lu for matching destinataires
            var rows = await _db.Alerte
                .Where(a => a.AlertRecordId == payload.AlerteId && (
                    (payload.Kind == "wa" && a.DestinatairePhoneNumber == payload.Value) ||
                    (payload.Kind == "email" && a.DestinataireEmail == payload.Value)))
                .ToListAsync();

            var confirmedCount = 0;
            foreach (var h in rows)
            {
                if (h.EtatAlerteId != 2) // Only update if not already confirmed
                {
                    h.EtatAlerteId = 2; // Lu
                    h.DateLecture = DateTime.UtcNow;
                    confirmedCount++;
                }
            }

            await _db.SaveChangesAsync();

            // Broadcast real-time update
            try
            {
                var hub = HttpContext.RequestServices.GetService<IHubContext<AlertSystem.Infrastructure.Hubs.NotificationHub>>();
                if (hub != null && alerte.DestinataireUserId.HasValue)
                {
                    await hub.Clients.Group($"user_{alerte.DestinataireUserId.Value}").SendCoreAsync(
                        "ReceiveNotification",
                        new object[] { "AlertProcessed", new { alertId = alerte.AlertRecordId, status = "Lu" } });
                    // KPIs refresh for the recipient
                    await hub.Clients.Group($"user_{alerte.DestinataireUserId.Value}").SendAsync("UpdateKpis");
                    // KPIs refresh for the sender as well
                    if (alerte.ExpediteurId.HasValue)
                    {
                        await hub.Clients.Group($"user_{alerte.ExpediteurId.Value}").SendAsync("UpdateKpis");
                    }
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


