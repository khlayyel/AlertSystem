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

        // Lightweight DTOs for list rendering
        private sealed record AlertListItemDto(
            int Id,
            string? Title,
            string? Message,
            int? AlertTypeId,
            int? StatutId,
            int? EtatAlerteId,
            DateTime DateCreation,
            string? SenderName
        );

        [HttpGet("GetInboxAlerts")]
        public async Task<IActionResult> GetInboxAlerts(DateTime? startDate = null, DateTime? endDate = null, int? typeId = null, int? stateId = null)
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Json(new { alerts = Array.Empty<AlertListItemDto>(), total = 0, error = "User not authenticated" });
            }

            var q = _db.Alerte
                .AsNoTracking()
                .Where(a => a.DestinataireUserId == currentUserId.Value);

            if (startDate.HasValue) q = q.Where(a => a.DateCreationAlerte >= startDate.Value);
            if (endDate.HasValue) q = q.Where(a => a.DateCreationAlerte < endDate.Value);
            if (typeId.HasValue) q = q.Where(a => a.AlertTypeId == typeId.Value);
            if (stateId.HasValue) q = q.Where(a => a.EtatAlerteId == stateId.Value);

            var items = await q
                .OrderByDescending(a => a.DateCreationAlerte)
                .Take(200)
                .Select(a => new AlertListItemDto(
                    a.AlertRecordId,
                    a.TitreAlerte,
                    a.DescriptionAlerte,
                    a.AlertTypeId,
                    a.StatutId,
                    a.EtatAlerteId,
                    a.DateCreationAlerte,
                    _db.DefUtilisateurs.Where(u => u.util_id == a.ExpediteurId).Select(u => (u.util_prenom+" "+u.util_nom).Trim()).FirstOrDefault()
                ))
                .ToListAsync();

            return Json(new { alerts = items, total = items.Count });
        }

        [HttpGet("GetOutboxAlerts")]
        public async Task<IActionResult> GetOutboxAlerts(DateTime? startDate = null, DateTime? endDate = null, int? typeId = null, int? stateId = null)
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Json(new { alerts = Array.Empty<AlertListItemDto>(), total = 0, error = "User not authenticated" });
            }

            var q = _db.Alerte
                .AsNoTracking()
                .Where(a => a.ExpediteurId == currentUserId.Value);

            if (startDate.HasValue) q = q.Where(a => a.DateCreationAlerte >= startDate.Value);
            if (endDate.HasValue) q = q.Where(a => a.DateCreationAlerte < endDate.Value);
            if (typeId.HasValue) q = q.Where(a => a.AlertTypeId == typeId.Value);
            if (stateId.HasValue) q = q.Where(a => a.StatutId == stateId.Value || a.EtatAlerteId == stateId.Value);

            var items = await q
                .OrderByDescending(a => a.DateCreationAlerte)
                .Take(200)
                .Select(a => new AlertListItemDto(
                    a.AlertRecordId,
                    a.TitreAlerte,
                    a.DescriptionAlerte,
                    a.AlertTypeId,
                    a.StatutId,
                    a.EtatAlerteId,
                    a.DateCreationAlerte,
                    _db.DefUtilisateurs.Where(u => u.util_id == a.ExpediteurId).Select(u => (u.util_prenom+" "+u.util_nom).Trim()).FirstOrDefault()
                ))
                .ToListAsync();

            return Json(new { alerts = items, total = items.Count });
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
                    .Select(a => a.AlertGroupId)
                    .Distinct()
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
        public async Task<IActionResult> GetUsers2()
        {
            var users = await _db.DefUtilisateurs
                .AsNoTracking()
                .Select(u => new
                {
                    id = u.util_id,
                    userId = u.util_id,
                    name = (u.util_prenom + " " + u.util_nom).Trim(),
                    email = u.util_email,
                    phoneNumber = (string?)null
                })
                .Take(500)
                .ToListAsync();

            return Json(new { success = true, users });
        }

        

        // Removed legacy duplicate endpoints to avoid route conflicts

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
                // Load the group id for this alert, then group recipients by user and collapse platforms
                var group = await _db.Alerte.Where(x => x.AlertRecordId == alerteId)
                    .Select(x => x.AlertGroupId).FirstOrDefaultAsync();
                if (group == Guid.Empty)
                {
                    return Json(new { recipients = new object[0] });
                }

                var recipients = await _db.Alerte
                    .Include(a => a.DestinataireUser)
                    .Where(a => a.AlertGroupId == group)
                    .GroupBy(a => new { a.DestinataireUserId, Name = a.DestinataireUser != null ? (a.DestinataireUser.util_prenom + " " + a.DestinataireUser.util_nom).Trim() : (string?)null })
                    .Select(g => new
                    {
                        recipientUserId = g.Key.DestinataireUserId,
                        recipientName = g.Key.Name ?? ("User " + g.Key.DestinataireUserId),
                        isRead = g.Any(x => x.EtatAlerteId == 2),
                        readDate = g.Where(x => x.DateLecture != null).OrderBy(x => x.DateLecture).Select(x => x.DateLecture).FirstOrDefault(),
                        status = g.Any(x => x.EtatAlerteId == 2) ? "Lu" : "Non Lu"
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

