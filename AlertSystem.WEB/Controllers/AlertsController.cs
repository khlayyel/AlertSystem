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
                .Include(x => x.TypeEnvoie)
                .Include(x => x.Statut)
                .Include(x => x.Etat)
                .Include(x => x.PlateformeEnvoie)
                .AsNoTracking()
                .Where(x => x.AlertRecordId == id)
                .Select(x => new {
                    id = x.AlertRecordId,
                    groupId = x.AlertGroupId,
                    title = x.TitreAlerte,
                    message = x.DescriptionAlerte,
                    type = x.TypeEnvoie != null ? x.TypeEnvoie.Description : null,
                    alertTypeId = x.TypeEnvoieId,
                    status = x.Statut != null ? x.Statut.Description : null,
                    statutId = x.StatutId,
                    etatId = x.EtatId,
                    createdAt = x.DateCreationAlerte,
                    readAt = x.DateLecture,
                    platform = x.PlateformeEnvoie != null ? x.PlateformeEnvoie.Description : null
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
                var row = await _db.Alerte.FirstOrDefaultAsync(a => a.AlertRecordId == alertRecipientId);
                if (row == null)
                {
                    return NotFound(new { success = false, message = "Alerte introuvable" });
                }

                row.EtatId = 2; // Lu
                row.DateLecture = DateTime.UtcNow;
                await _db.SaveChangesAsync();

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
                        await hub.Clients.All.SendAsync("ReceiveNotification", "UpdateKpis", null);
                        await hub.Clients.All.SendAsync("ReceiveNotification", "AlertStatusUpdated", new { groupId = row.AlertGroupId, status = "Lu", readAt = DateTime.UtcNow });
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
