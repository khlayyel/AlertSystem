using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service;

namespace AlertSystem.WEB.Controllers
{
    public sealed class AlertsController : Controller
    {
        private readonly IAlertReadService _alertReadService;

        public AlertsController(IAlertReadService alertReadService)
        {
            _alertReadService = alertReadService;
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
    }
}
