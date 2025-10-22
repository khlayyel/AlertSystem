using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service;
using AlertSystem.Services;
using Hangfire;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using AlertSystem.WEB.Services;

namespace AlertSystem.WEB.Controllers
{
    public sealed class AlertsCrudController : Controller
    {
        private readonly IAlertCrudService _service;
        private readonly IAlertSendService _sendService;
        private readonly ICurrentUserAccessor _currentUser;
        private readonly ILogger<AlertsCrudController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IDelayedAlertJobService _delayedJobService;

        public AlertsCrudController(
            IAlertCrudService service, 
            IAlertSendService sendService, 
            ICurrentUserAccessor currentUser, 
            ILogger<AlertsCrudController> logger,
            ApplicationDbContext db,
            IDelayedAlertJobService delayedJobService)
        {
            _service = service;
            _sendService = sendService;
            _currentUser = currentUser;
            _logger = logger;
            _db = db;
            _delayedJobService = delayedJobService;
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
            try
            {
                Console.WriteLine($"AlertsCrudController.Send called with title: {dto.Title}, emails: {dto.Emails?.Length ?? 0}, platforms: {dto.Platforms?.Email}");
                if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("title required");
                if (string.IsNullOrWhiteSpace(dto.Message)) dto.Message = string.Empty;

                // Desktop fallback: if Desktop selected and no userIds provided, use current logged-in user
                int[]? userIds = dto.UserIds;
                if ((dto.Platforms?.Desktop ?? false) && (userIds == null || userIds.Length == 0))
                {
                    var uid = _currentUser.GetUserId();
                    if (uid.HasValue && uid.Value > 0)
                    {
                        userIds = new[] { uid.Value };
                    }
                }

                // Create alert in database first (with pending status)
                var alert = new AlertSystem.Entities.Entities.Alerte
                {
                    TitreAlerte = dto.Title,
                    DescriptionAlerte = dto.Message,
                    DateCreationAlerte = DateTime.UtcNow,
                    StatutId = 1, // En Cours (pending)
                    AlertTypeId = dto.AlertTypeId ?? 1,
                    ExpedTypeId = 1, // Service
                    EtatAlerteId = 1 // Non Lu
                };

                _db.Alerte.Add(alert);
                await _db.SaveChangesAsync();

                // Create HistoriqueAlerte entries for recipients
                var historiqueEntries = new List<AlertSystem.Entities.Entities.HistoriqueAlerte>();

                // Add email recipients
                if (dto.Platforms?.Email == true && dto.Emails != null)
                {
                    foreach (var email in dto.Emails)
                    {
                        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
                        historiqueEntries.Add(new AlertSystem.Entities.Entities.HistoriqueAlerte
                        {
                            AlerteId = alert.AlerteId,
                            DestinataireEmail = email,
                            DestinataireUserId = user?.UserId,
                            EtatAlerteId = 1, // Non Lu
                            DateLecture = null,
                            PlateformeEnvoieId = 1 // Email
                        });
                    }
                }

                // Add WhatsApp recipients
                if (dto.Platforms?.WhatsApp == true && dto.Phones != null)
                {
                    foreach (var phone in dto.Phones)
                    {
                        var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
                        historiqueEntries.Add(new AlertSystem.Entities.Entities.HistoriqueAlerte
                        {
                            AlerteId = alert.AlerteId,
                            DestinatairePhoneNumber = phone,
                            DestinataireUserId = user?.UserId,
                            EtatAlerteId = 1, // Non Lu
                            DateLecture = null,
                            PlateformeEnvoieId = 2 // WhatsApp
                        });
                    }
                }

                // Add desktop recipients
                if (dto.Platforms?.Desktop == true && userIds != null)
                {
                    foreach (var userId in userIds)
                    {
                        var user = await _db.Users.FindAsync(userId);
                        historiqueEntries.Add(new AlertSystem.Entities.Entities.HistoriqueAlerte
                        {
                            AlerteId = alert.AlerteId,
                            DestinataireUserId = userId,
                            DestinataireDesktop = user?.DesktopDeviceToken,
                            EtatAlerteId = 1, // Non Lu
                            DateLecture = null,
                            PlateformeEnvoieId = 3 // Desktop
                        });
                    }
                }

                if (historiqueEntries.Any())
                {
                    _db.HistoriqueAlertes.AddRange(historiqueEntries);
                    await _db.SaveChangesAsync();
                }

                // Schedule the actual send for 5 seconds later
                var jobId = BackgroundJob.Schedule(() => _delayedJobService.ExecuteDelayedSendAsync(alert.AlerteId), TimeSpan.FromSeconds(5));

                return Ok(new { 
                    success = true, 
                    alerteId = alert.AlerteId,
                    jobId = jobId,
                    message = "Alert scheduled for sending in 5 seconds"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertsCrud/Send failed with exception");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("CancelSend/{alerteId}")]
        public async Task<IActionResult> CancelSend(int alerteId)
        {
            try
            {
                var alert = await _db.Alerte.FindAsync(alerteId);
                if (alert == null)
                {
                    return NotFound("Alert not found");
                }

                // Get the job ID from the alert
                var jobId = alert.PlateformeEnvoieId?.ToString();
                if (!string.IsNullOrEmpty(jobId))
                {
                    // Delete the scheduled job
                    BackgroundJob.Delete(jobId);
                }

                // Update alert status to cancelled
                alert.StatutId = 3; // Annulé
                alert.PlateformeEnvoieId = null; // Clear job ID
                await _db.SaveChangesAsync();

                _logger.LogInformation("Alert {AlerteId} send cancelled", alerteId);

                return Ok(new { success = true, message = "Send cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel send for alert {AlerteId}", alerteId);
                return StatusCode(500, ex.Message);
            }
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
