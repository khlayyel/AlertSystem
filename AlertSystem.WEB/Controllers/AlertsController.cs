using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using AlertSystem.Services;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using AlertSystem.Infrastructure.Hubs;

namespace AlertSystem.WEB.Controllers
{
    [Authorize]
    public sealed class AlertsController : Controller
    {
        private readonly AlertReadService _AlertReadService;
        private readonly KpiUpdateService _KpiUpdateService;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ApplicationDbContext _db;

        public AlertsController(AlertReadService AlertReadService, KpiUpdateService KpiUpdateService, ICurrentUserAccessor currentUserAccessor, ApplicationDbContext db)
        {
            _AlertReadService = AlertReadService;
            _KpiUpdateService = KpiUpdateService;
            _currentUserAccessor = currentUserAccessor;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var count = await _AlertReadService.GetUnreadCountAsync();
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> TodayCount()
        {
            var count = await _AlertReadService.GetTodayCountAsync();
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> MandatoryPendingCount()
        {
            var count = await _AlertReadService.GetUnconfirmedMandatoryCountAsync();
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmedMandatoryCount()
        {
            var count = await _AlertReadService.GetConfirmedMandatoryCountAsync();
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> InboxData(string status = "all", int page = 1, int size = 10)
        {
            var result = await _AlertReadService.GetUnreadAlertsAsync(page, size);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> HistoryData(string status = "all", int page = 1, int size = 10)
        {
            var result = await _AlertReadService.GetAlertsByDateRangeAsync(DateTime.Today.AddDays(-30), DateTime.Today);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> SentData(int page = 1, int size = 10)
        {
            var result = await _AlertReadService.GetAlertsByDateRangeAsync(DateTime.Today.AddDays(-30), DateTime.Today);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var currentUserId = _currentUserAccessor.GetUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var a = await _db.Alerte
                .Include(x => x.AlertType)
                .Include(x => x.Statut)
                .Include(x => x.Etat)
                .Include(x => x.PlateformeEnvoie)
                .AsNoTracking()
                .Where(x => x.AlertRecordId == id && (x.ExpediteurId == currentUserId.Value || x.DestinataireUserId == currentUserId.Value))
                .Select(x => new {
                    id = x.AlertRecordId,
                    groupId = x.AlertGroupId,
                    title = x.TitreAlerte,
                    message = x.DescriptionAlerte,
                    type = x.AlertType != null ? x.AlertType.AlertTypeName : null,
                    alertTypeId = x.AlertTypeId,
                    status = x.Statut != null ? x.Statut.StatutName : null,
                    statutId = x.StatutId,
                    etatAlerteId = x.EtatAlerteId,
                    createdAt = x.DateCreationAlerte,
                    readAt = x.DateLecture,
                    platform = x.PlateformeEnvoie != null ? x.PlateformeEnvoie.Plateforme : null
                })
                .FirstOrDefaultAsync();

            if (a == null) return NotFound();
            return Json(a);
        }


        [HttpPost]
        public async Task<IActionResult> MarkRead([FromForm] int alertRecipientId)
        {
            try
            {
                // Load the target alert to get group and user
                var target = await _db.Alerte.AsNoTracking()
                    .Where(a => a.AlertRecordId == alertRecipientId)
                    .Select(a => new { a.AlertGroupId, a.DestinataireUserId, a.ExpediteurId })
                    .FirstOrDefaultAsync();
                if (target == null)
                {
                    return NotFound(new { success = false, message = "Alerte introuvable" });
                }

                // Update all rows in the same group for this recipient user (per-user confirmation)
                var rows = await _db.Alerte
                    .Where(a => a.AlertGroupId == target.AlertGroupId && a.DestinataireUserId == target.DestinataireUserId)
                    .ToListAsync();

                if (!rows.Any())
                {
                    return NotFound(new { success = false, message = "Aucune ligne destinataire pour cette alerte" });
                }

                foreach (var r in rows)
                {
                    r.EtatAlerteId = 2; // Lu
                    r.DateLecture = DateTime.UtcNow;
                }
                await _db.SaveChangesAsync();

                // Real-time updates: KPIs and status change to both recipient and sender
                var currentUserId = _currentUserAccessor.GetUserId();
                if (currentUserId.HasValue)
                {
                    await _KpiUpdateService.SendInboxKpiUpdateAsync(currentUserId.Value);
                }

                try
                {
                    var hub = HttpContext.RequestServices.GetService<IHubContext<AlertSystem.Infrastructure.Hubs.NotificationHub>>();
                    if (hub != null)
                    {
                        if (target.DestinataireUserId.HasValue)
                        {
                            await hub.Clients.Group($"user_{target.DestinataireUserId.Value}").SendAsync("ReceiveNotification", "UpdateKpis", null);
                            await hub.Clients.Group($"user_{target.DestinataireUserId.Value}").SendAsync("ReceiveNotification", "AlertStatusUpdated", new { groupId = target.AlertGroupId, userId = target.DestinataireUserId.Value, status = "Lu", readAt = DateTime.UtcNow });
                        }
                        if (target.ExpediteurId.HasValue)
                        {
                            await hub.Clients.Group($"user_{target.ExpediteurId.Value}").SendAsync("ReceiveNotification", "UpdateKpis", null);
                            await hub.Clients.Group($"user_{target.ExpediteurId.Value}").SendAsync("ReceiveNotification", "AlertStatusUpdated", new { groupId = target.AlertGroupId, userId = target.DestinataireUserId, status = "Lu", readAt = DateTime.UtcNow });
                        }
                    }
                }
                catch { }

                return Json(new { success = true, message = "Alerte marquée comme lue" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Erreur interne du serveur", error = ex.Message });
            }
        }
    }
}
