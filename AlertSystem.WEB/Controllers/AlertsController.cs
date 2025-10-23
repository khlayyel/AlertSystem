using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service;
using AlertSystem.Services;

namespace AlertSystem.WEB.Controllers
{
    public sealed class AlertsController : Controller
    {
        private readonly IAlertReadService _alertReadService;
        private readonly IKpiUpdateService _kpiUpdateService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public AlertsController(IAlertReadService alertReadService, IKpiUpdateService kpiUpdateService, ICurrentUserAccessor currentUserAccessor)
        {
            _alertReadService = alertReadService;
            _kpiUpdateService = kpiUpdateService;
            _currentUserAccessor = currentUserAccessor;
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var count = await _alertReadService.GetUnreadCountAsync();
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> TodayCount()
        {
            var count = await _alertReadService.GetTodayCountAsync();
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> MandatoryPendingCount()
        {
            var count = await _alertReadService.GetMandatoryPendingCountAsync();
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmedMandatoryCount()
        {
            var count = await _alertReadService.GetConfirmedMandatoryCountAsync();
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> InboxData(string status = "all", int page = 1, int size = 10)
        {
            var result = await _alertReadService.GetInboxAsync(page, size, status);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> HistoryData(string status = "all", int page = 1, int size = 10)
        {
            var result = await _alertReadService.GetHistoryAsync(status, page, size);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> SentData(int page = 1, int size = 10)
        {
            var result = await _alertReadService.GetSentAsync(page, size);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _alertReadService.GetDetailsAsync(id);
            if (result == null) return NotFound();
            return Json(result);
        }


        [HttpPost]
        public async Task<IActionResult> MarkRead([FromForm] int alertRecipientId)
        {
            try
            {
                Console.WriteLine($"MarkRead: Received alertRecipientId: {alertRecipientId}");
                var success = await _alertReadService.MarkAsReadAsync(alertRecipientId);
                if (success)
                {
                    Console.WriteLine($"MarkRead: Successfully marked alert {alertRecipientId} as read");
                    
                    // Send real-time KPI update
                    var currentUserId = _currentUserAccessor.GetUserId();
                    if (currentUserId.HasValue)
                    {
                        await _kpiUpdateService.SendInboxKpiUpdateAsync(currentUserId.Value);
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
