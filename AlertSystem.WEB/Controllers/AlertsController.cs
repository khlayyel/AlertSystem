using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using AlertSystem.Services;

namespace AlertSystem.WEB.Controllers
{
    [Authorize]
    public sealed class AlertsController : Controller
    {
        private readonly AlertReadService _AlertReadService;
        private readonly KpiUpdateService _KpiUpdateService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public AlertsController(AlertReadService AlertReadService, KpiUpdateService KpiUpdateService, ICurrentUserAccessor currentUserAccessor)
        {
            _AlertReadService = AlertReadService;
            _KpiUpdateService = KpiUpdateService;
            _currentUserAccessor = currentUserAccessor;
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
            var result = await _AlertReadService.GetAlertsByDateRangeAsync(DateTime.Today.AddDays(-30), DateTime.Today);
            if (result == null) return NotFound();
            return Json(result);
        }


        [HttpPost]
        public async Task<IActionResult> MarkRead([FromForm] int alertRecipientId)
        {
            try
            {
                Console.WriteLine($"MarkRead: Received alertRecipientId: {alertRecipientId}");
                var success = await _AlertReadService.MarkAsReadAsync(alertRecipientId);
                if (success)
                {
                    Console.WriteLine($"MarkRead: Successfully marked alert {alertRecipientId} as read");
                    
                    // Send real-time KPI update
                    var currentUserId = _currentUserAccessor.GetUserId();
                    if (currentUserId.HasValue)
                    {
                        await _KpiUpdateService.SendInboxKpiUpdateAsync(currentUserId.Value);
                    }
                    
                    return Json(new { success = true, message = "Alerte marquée comme lue" });
                }
                else
                {
                    Console.WriteLine($"MarkRead: Failed to mark alert {alertRecipientId} as read");
                    return BadRequest(new { success = false, message = "Impossible de marquer l'alerte comme lue" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MarkRead: Exception occurred: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Erreur interne du serveur", error = ex.Message });
            }
        }
    }
}
