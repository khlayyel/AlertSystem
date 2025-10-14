using AlertSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlertSystem.Controllers
{
    public sealed class StatisticsController : Controller
    {
        private readonly AlertAuditService _auditService;
        private readonly ReminderConfiguration _reminderConfig;

        public StatisticsController(AlertAuditService auditService, ReminderConfiguration reminderConfig)
        {
            _auditService = auditService;
            _reminderConfig = reminderConfig;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStatistics(DateTime? from = null, DateTime? to = null)
        {
            from ??= DateTime.UtcNow.AddDays(-30); // Default: last 30 days
            to ??= DateTime.UtcNow;

            var stats = await _auditService.GetAlertStatistics(from.Value, to.Value);
            return Json(stats);
        }

        [HttpGet]
        public IActionResult GetReminderConfiguration()
        {
            return Json(new
            {
                FirstReminderDelay = _reminderConfig.FirstReminderDelay.TotalMinutes,
                SubsequentReminderInterval = _reminderConfig.SubsequentReminderInterval.TotalMinutes,
                MaxReminders = _reminderConfig.MaxReminders,
                ServiceCheckInterval = _reminderConfig.ServiceCheckInterval.TotalMinutes,
                EnableReminders = _reminderConfig.EnableReminders,
                AlertTypeIntervals = _reminderConfig.AlertTypeIntervals.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value.TotalMinutes)
            });
        }

        [HttpPost]
        public IActionResult UpdateReminderConfiguration([FromBody] ReminderConfigurationUpdate update)
        {
            if (update.FirstReminderDelayMinutes.HasValue)
                _reminderConfig.FirstReminderDelay = TimeSpan.FromMinutes(update.FirstReminderDelayMinutes.Value);
            
            if (update.SubsequentReminderIntervalMinutes.HasValue)
                _reminderConfig.SubsequentReminderInterval = TimeSpan.FromMinutes(update.SubsequentReminderIntervalMinutes.Value);
            
            if (update.MaxReminders.HasValue)
                _reminderConfig.MaxReminders = update.MaxReminders.Value;
            
            if (update.EnableReminders.HasValue)
                _reminderConfig.EnableReminders = update.EnableReminders.Value;

            return Json(new { success = true, message = "Configuration mise à jour avec succès" });
        }
    }

    public class ReminderConfigurationUpdate
    {
        public int? FirstReminderDelayMinutes { get; set; }
        public int? SubsequentReminderIntervalMinutes { get; set; }
        public int? MaxReminders { get; set; }
        public bool? EnableReminders { get; set; }
    }
}
