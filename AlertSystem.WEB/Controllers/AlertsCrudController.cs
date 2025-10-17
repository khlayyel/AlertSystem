using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service;
using AlertSystem.Services;

namespace AlertSystem.WEB.Controllers
{
    public sealed class AlertsCrudController : Controller
    {
        private readonly IAlertCrudService _service;
        private readonly IAlertSendService _sendService;

        public AlertsCrudController(IAlertCrudService service, IAlertSendService sendService)
        {
            _service = service;
            _sendService = sendService;
        }

        [HttpGet]
        public async Task<IActionResult> QuickList()
        {
            var quickAlerts = await _service.GetQuickListAsync();
            return Json(quickAlerts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFromTemplate([FromBody] CreateFromTemplateDto dto)
        {
            var (success, alertId, error) = await _service.CreateFromTemplateAsync(dto.Title, dto.Message, dto.Type);
            if (!success) return BadRequest(new { error = error ?? "Failed to create alert" });
            return Json(new { success = true, alertId });
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuick([FromBody] CreateFromTemplateDto dto)
        {
            var (success, alertId, error) = await _service.CreateFromTemplateAsync(dto.Title, dto.Message, dto.Type);
            if (!success) return BadRequest(new { error = error ?? "Failed to save quick alert" });
            return Json(new { success = true, alertId });
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("title required");
            if (string.IsNullOrWhiteSpace(dto.Message)) dto.Message = string.Empty;

            var (alerteId, ok) = await _sendService.SendManualAsync(
                dto.Title,
                dto.Message,
                dto.Emails ?? Array.Empty<string>(),
                dto.Phones ?? Array.Empty<string>(),
                dto.Platforms?.Email ?? false,
                dto.Platforms?.WhatsApp ?? false,
                dto.Platforms?.Desktop ?? false,
                dto.UserIds,
                dto.AlertTypeId
            );
            return Json(new { success = ok, alerteId });
        }

        public sealed class CreateFromTemplateDto
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Type { get; set; } = "Information";
        }

        public sealed class SendDto
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string[]? Emails { get; set; }
            public string[]? Phones { get; set; }
            public int[]? UserIds { get; set; }
            public PlatformsDto? Platforms { get; set; }
            public int? AlertTypeId { get; set; }
        }

        public sealed class PlatformsDto
        {
            public bool Email { get; set; }
            public bool WhatsApp { get; set; }
            public bool Desktop { get; set; }
        }
    }
}
