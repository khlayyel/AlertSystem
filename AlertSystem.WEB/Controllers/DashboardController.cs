using System.Security.Claims;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;
using AlertSystem.WEB.Services;

namespace AlertSystem.WEB.Controllers
{
    [Authorize]
    [Route("Dashboard")]
    public sealed class DashboardController : Controller
    {
        private readonly AlertReadService _read;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        
        public DashboardController(AlertReadService read, ApplicationDbContext db, ICurrentUserService currentUserService) 
        { 
            _read = read; 
            _db = db;
            _currentUserService = currentUserService;
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
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Json(new { receivedToday = 0, unreadAlerts = 0, pendingConfirmation = 0, error = "User not authenticated" });
                }

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Reçues aujourd'hui: nombre d'alertes reçues par l'utilisateur aujourd'hui
                var receivedToday = await _db.Alerte
                    .Where(a => a.DestinataireUserId == currentUserId.Value && 
                               a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow)
                    .CountAsync();

                // Non lues (EtatAlerteId = 1) reçues par l'utilisateur
                var unreadAlerts = await _db.Alerte
                    .Where(a => a.DestinataireUserId == currentUserId.Value && a.EtatAlerteId == 1)
                    .CountAsync();

                // En attente de confirmation (acquittementNécessaire non confirmées) reçues par l'utilisateur
                var pendingConfirmation = await _db.Alerte
                    .Where(a => a.DestinataireUserId == currentUserId.Value && a.EtatAlerteId == 1 && a.AlertTypeId == 3)
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
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Json(new { sentToday = 0, confirmedAlerts = 0, pendingConfirmation = 0, error = "User not authenticated" });
                }

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Envoyées aujourd'hui: nombre d'alertes envoyées par l'utilisateur aujourd'hui
                var sentToday = await _db.Alerte
                    .Where(a => a.ExpediteurId == currentUserId.Value && 
                               a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow)
                    .CountAsync();

                // Confirmées: alertes envoyées par l'utilisateur et lues par au moins un destinataire
                var confirmedAlerts = await _db.Alerte
                    .Where(a => a.ExpediteurId == currentUserId.Value && a.EtatAlerteId == 2)
                    .CountAsync();

                // En attente de confirmation: acquittementNécessaire envoyées par l'utilisateur avec au moins un destinataire non lu
                var pendingConfirmation = await _db.Alerte
                    .Where(a => a.ExpediteurId == currentUserId.Value && a.AlertTypeId == 3 && a.EtatAlerteId == 1)
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
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Json(new { inboxUnreadCount = 0, outboxPendingCount = 0, error = "User not authenticated" });
                }

                // Nombre d'alertes non lues reçues par l'utilisateur
                var inboxUnreadCount = await _db.Alerte
                    .Where(a => a.DestinataireUserId == currentUserId.Value && a.EtatAlerteId == 1)
                    .CountAsync();

                // Nombre d'alertes en attente envoyées par l'utilisateur
                var outboxPendingCount = await _db.Alerte
                    .Where(a => a.ExpediteurId == currentUserId.Value && a.AlertTypeId == 3 && a.EtatAlerteId == 1)
                    .CountAsync();

                return Json(new { inboxUnreadCount, outboxPendingCount });
            }
            catch (Exception ex)
            {
                return Json(new { inboxUnreadCount = 0, outboxPendingCount = 0, error = ex.Message });
            }
        }

        

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                // Return all active users regardless of current auth state to populate the compose dialog reliably
                var activeUsers = await _db.DefUtilisateurs
                    .Where(u => u.util_compte_active)
                    .Select(u => new
                    {
                        id = u.util_id,
                        name = (u.util_prenom + " " + u.util_nom).Trim(),
                        email = u.util_email,
                        login = u.util_login
                    })
                    .OrderBy(u => u.name)
                    .ToListAsync();

                return Json(new { success = true, users = activeUsers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetInboxAlerts")]
        public async Task<IActionResult> GetInboxAlerts()
        {
            try
            {
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var alerts = await _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.Statut)
                    .Include(a => a.Etat)
                    .Include(a => a.PlateformeEnvoie)
                    .Where(a => a.DestinataireUserId == currentUserId.Value)
                    .OrderByDescending(a => a.DateCreationAlerte)
                    .Select(a => new
                    {
                        id = a.AlertRecordId,
                        title = a.TitreAlerte,
                        message = a.DescriptionAlerte,
                        date = a.DateCreationAlerte,
                        status = a.Statut.StatutName,
                        state = a.Etat.EtatAlerteName,
                        platform = a.PlateformeEnvoie.Plateforme,
                        alertType = a.AlertType.AlertTypeName,
                        senderId = a.ExpediteurId
                    })
                    .ToListAsync();

                return Json(new { success = true, alerts = alerts });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetOutboxAlerts")]
        public async Task<IActionResult> GetOutboxAlerts()
        {
            try
            {
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var alerts = await _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.Statut)
                    .Include(a => a.Etat)
                    .Include(a => a.PlateformeEnvoie)
                    .Where(a => a.ExpediteurId == currentUserId.Value)
                    .OrderByDescending(a => a.DateCreationAlerte)
                    .Select(a => new
                    {
                        id = a.AlertRecordId,
                        title = a.TitreAlerte,
                        message = a.DescriptionAlerte,
                        date = a.DateCreationAlerte,
                        status = a.Statut.StatutName,
                        state = a.Etat.EtatAlerteName,
                        platform = a.PlateformeEnvoie.Plateforme,
                        alertType = a.AlertType.AlertTypeName,
                        recipientId = a.DestinataireUserId,
                        recipientEmail = a.DestinataireEmail,
                        recipientPhone = a.DestinatairePhoneNumber
                    })
                    .ToListAsync();

                return Json(new { success = true, alerts = alerts });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("ConfirmAlert/{alertId}")]
        public async Task<IActionResult> ConfirmAlert(int alertId)
        {
            try
            {
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var alert = await _db.Alerte
                    .FirstOrDefaultAsync(a => a.AlertRecordId == alertId && a.DestinataireUserId == currentUserId.Value);

                if (alert == null)
                {
                    return Json(new { success = false, message = "Alert not found or not accessible" });
                }

                // Mark as read
                alert.EtatAlerteId = 2; // Lu
                alert.DateLecture = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Alert confirmed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("AlertRecipients/{alerteId}")]
        public async Task<IActionResult> GetAlertRecipients(int alerteId)
        {
            try
            {
                var recipients = await _db.Alerte
                    .Include(a => a.PlateformeEnvoie)
                    .Include(a => a.DestinataireUser)
                    .Where(a => a.AlertRecordId == alerteId)
                    .Select(a => new
                    {
                        recipientId = a.AlertRecordId,
                        recipientEmail = a.DestinataireEmail,
                        recipientPhone = a.DestinatairePhoneNumber,
                        recipientDesktop = a.DestinataireDesktop,
                        recipientUserId = a.DestinataireUserId,
                        recipientName = a.DestinataireUser != null ? a.DestinataireUser.util_nom : null,
                        platform = a.PlateformeEnvoie != null ? a.PlateformeEnvoie.Plateforme : "Inconnu",
                        status = a.EtatAlerteId == 1 ? "Non Lu" : a.EtatAlerteId == 2 ? "Lu" : "Inconnu",
                        readDate = a.DateLecture,
                        isRead = a.EtatAlerteId == 2 || a.DateLecture != null,
                        etatAlerteId = a.EtatAlerteId
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

