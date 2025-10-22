using System.Security.Claims;
using AlertSystem.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;

namespace AlertSystem.WEB.Controllers
{
    // Auth temporairement désactivée pendant la refonte
    [Route("Dashboard")]
    public sealed class DashboardController : Controller
    {
        private readonly IAlertReadService _read;
        private readonly ApplicationDbContext _db;
        
        public DashboardController(IAlertReadService read, ApplicationDbContext db) 
        { 
            _read = read; 
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Redirect to Inbox as default view
            return RedirectToAction("Inbox");
        }

        [HttpGet("Inbox")]
        public IActionResult Inbox()
        {
            return View();
        }

        [HttpGet("Sent")]
        public IActionResult Sent()
        {
            return View();
        }

        [HttpGet("InboxKpiData")]
        public async Task<IActionResult> GetInboxKpiData()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Reçues aujourd'hui: nombre d'entrées d'historique liées à des alertes créées aujourd'hui
                var receivedToday = await _db.HistoriqueAlertes
                    .Include(h => h.Alerte)
                    .Where(h => h.Alerte != null && h.Alerte.DateCreationAlerte >= today && h.Alerte.DateCreationAlerte < tomorrow)
                    .CountAsync();

                // Non lues (EtatAlerteId = 1)
                var unreadAlerts = await _db.HistoriqueAlertes
                    .Where(h => h.EtatAlerteId == 1)
                    .CountAsync();

                // En attente de confirmation (acquittementNécessaire non confirmées)
                var pendingConfirmation = await _db.HistoriqueAlertes
                    .Include(h => h.Alerte)
                    .Where(h => h.EtatAlerteId == 1 && h.Alerte != null && h.Alerte.AlertTypeId == 3)
                    .CountAsync();

                return Json(new { receivedToday, unreadAlerts, pendingConfirmation });
            }
            catch (Exception ex)
            {
                return Json(new { receivedToday = 0, unreadAlerts = 0, pendingConfirmation = 0, error = ex.Message });
            }
        }

        [HttpGet("OutboxKpiData")]
        public async Task<IActionResult> GetOutboxKpiData()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Envoyées aujourd'hui: nombre d'alertes créées aujourd'hui
                var sentToday = await _db.Alerte
                    .Where(a => a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow)
                    .CountAsync();

                // Confirmées: au moins un destinataire a lu (EtatAlerteId = 2)
                var confirmedAlerts = await _db.Alerte
                    .Where(a => a.HistoriqueAlertes.Any(h => h.EtatAlerteId == 2))
                    .CountAsync();

                // En attente de confirmation: acquittementNécessaire avec au moins un destinataire non lu
                var pendingConfirmation = await _db.Alerte
                    .Where(a => a.AlertTypeId == 3 && a.HistoriqueAlertes.Any(h => h.EtatAlerteId == 1))
                    .CountAsync();

                return Json(new { sentToday, confirmedAlerts, pendingConfirmation });
            }
            catch (Exception ex)
            {
                return Json(new { sentToday = 0, confirmedAlerts = 0, pendingConfirmation = 0, error = ex.Message });
            }
        }

        [HttpGet("SidebarCounts")]
        public async Task<IActionResult> GetSidebarCounts()
        {
            try
            {
                var inboxUnreadCount = await _db.HistoriqueAlertes
                    .Where(h => h.EtatAlerteId == 1)
                    .CountAsync();

                var outboxPendingCount = await _db.Alerte
                    .Where(a => a.AlertTypeId == 3 && a.HistoriqueAlertes.Any(h => h.EtatAlerteId == 1))
                    .CountAsync();

                return Json(new { inboxUnreadCount, outboxPendingCount });
            }
            catch (Exception ex)
            {
                return Json(new { inboxUnreadCount = 0, outboxPendingCount = 0, error = ex.Message });
            }
        }

        

        [HttpGet("AlertRecipients/{alerteId}")]
        public async Task<IActionResult> GetAlertRecipients(int alerteId)
        {
            try
            {
                var recipients = await _db.HistoriqueAlertes
                    .Include(h => h.PlateformeEnvoie)
                    .Where(h => h.AlerteId == alerteId)
                    .Select(h => new
                    {
                        recipientId = h.DestinataireId,
                        recipientEmail = h.DestinataireEmail,
                        recipientPhone = h.DestinatairePhoneNumber,
                        recipientDesktop = h.DestinataireDesktop,
                        recipientUserId = h.DestinataireUserId,
                        recipientName = h.User != null ? h.User.FullName : null,
                        platform = h.PlateformeEnvoie != null ? h.PlateformeEnvoie.Plateforme : "Inconnu",
                        status = h.EtatAlerteId == 1 ? "Non Lu" : h.EtatAlerteId == 2 ? "Lu" : "Inconnu",
                        readDate = h.DateLecture,
                        isRead = h.EtatAlerteId == 2 || h.DateLecture != null,
                        etatAlerteId = h.EtatAlerteId
                    })
                    .ToListAsync();

                return Json(new { recipients });
            }
            catch (Exception ex)
            {
                return Json(new { recipients = new object[0], error = ex.Message });
            }
        }
    }
}

