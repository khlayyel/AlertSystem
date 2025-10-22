using Microsoft.AspNetCore.Mvc;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Services;
using System.Text.RegularExpressions;
using AlertSystem.Entities.Entities;
using AlertSystem.Service;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public sealed class AlertsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly IAlertSendService _alertSendService;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(ApplicationDbContext db, INotificationService notificationService, IAlertSendService alertSendService, ILogger<AlertsController> logger)
        { 
            _db = db; 
            _notificationService = notificationService;
            _alertSendService = alertSendService;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var alert = await _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.ExpedType)
                    .Include(a => a.Statut)
                    .Include(a => a.Etat)
                    .Include(a => a.HistoriqueAlertes)
                    .AsNoTracking()
                    .Where(x => x.AlerteId == id)
                    .Select(a => new
                    {
                        alerteId = a.AlerteId,
                        title = a.TitreAlerte,
                        description = a.DescriptionAlerte,
                        alertType = a.AlertType != null ? a.AlertType.AlertTypeName : null,
                        expedType = a.ExpedType != null ? a.ExpedType.ExpedTypeName : null,
                        statut = a.Statut != null ? a.Statut.StatutName : null,
                        etat = a.Etat != null ? a.Etat.EtatAlerteName : null,
                        dateCreation = a.DateCreationAlerte,
                        appId = a.AppId,
                        expediteurId = a.ExpediteurId,
                        recipients = a.HistoriqueAlertes.Select(d => new
                        {
                            destinataireId = d.DestinataireId,
                            destinataireUserId = d.DestinataireUserId,
                            etatAlerteId = d.EtatAlerteId,
                            dateLecture = d.DateLecture,
                            rappelSuivant = d.RappelSuivant,
                            destinataireEmail = d.DestinataireEmail,
                            destinatairePhoneNumber = d.DestinatairePhoneNumber,
                            destinataireDesktop = d.DestinataireDesktop
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (alert == null) return NotFound(new { error = "Alert not found" });
                return Ok(alert);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Query(string? type, DateTime? from, DateTime? to, string? status, int? appId, int page = 1, int size = 20, string sort = "dateCreation", string order = "desc")
        {
            try
            {
                var query = _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.ExpedType)
                    .Include(a => a.Statut)
                    .Include(a => a.Etat)
                    .AsNoTracking()
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(type))
                {
                    query = query.Where(x => x.AlertType != null && x.AlertType.AlertTypeName == type);
                }
                
                if (from.HasValue) 
                {
                    query = query.Where(x => x.DateCreationAlerte >= from.Value);
                }
                
                if (to.HasValue) 
                {
                    query = query.Where(x => x.DateCreationAlerte < to.Value.AddDays(1));
                }
                
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(x => x.Statut != null && x.Statut.StatutName == status);
                }
                
                if (appId.HasValue)
                {
                    query = query.Where(x => x.AppId == appId.Value);
                }

                var total = await query.CountAsync();

                // Apply sorting
                query = sort.ToLower() switch
                {
                    "title" => order.ToLower() == "asc" ? query.OrderBy(x => x.TitreAlerte) : query.OrderByDescending(x => x.TitreAlerte),
                    "type" => order.ToLower() == "asc" ? query.OrderBy(x => x.AlertType!.AlertTypeName) : query.OrderByDescending(x => x.AlertType!.AlertTypeName),
                    "status" => order.ToLower() == "asc" ? query.OrderBy(x => x.Statut!.StatutName) : query.OrderByDescending(x => x.Statut!.StatutName),
                    _ => order.ToLower() == "asc" ? query.OrderBy(x => x.DateCreationAlerte) : query.OrderByDescending(x => x.DateCreationAlerte)
                };

                var items = await query
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(a => new
                    {
                        alerteId = a.AlerteId,
                        title = a.TitreAlerte,
                        description = a.DescriptionAlerte,
                        alertType = a.AlertType != null ? a.AlertType.AlertTypeName : null,
                        expedType = a.ExpedType != null ? a.ExpedType.ExpedTypeName : null,
                        statut = a.Statut != null ? a.Statut.StatutName : null,
                        etat = a.Etat != null ? a.Etat.EtatAlerteName : null,
                        dateCreation = a.DateCreationAlerte,
                        appId = a.AppId,
                        expediteurId = a.ExpediteurId
                    })
                    .ToListAsync();

                return Ok(new { items, total, page, size, sort, order });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        public sealed class CreateAlertDto
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string AlertType { get; set; } = "acquittementNonNécessaire";
            public string ExpedType { get; set; } = "Service"; // Humain|Service
            public int AppId { get; set; } // obligatoire
            public int? ExpediteurId { get; set; }
            public RecipientDto[]? Recipients { get; set; }
        }

        public sealed class RecipientDto
        {
            public string? RecipientId { get; set; } // Renommé de ExternalRecipientId
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAlertDto dto)
        {
            try
            {
                _logger.LogInformation("[DEBUG] Creating alert: {Title}", dto.Title);

                // Validate input
                if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Message))
                    return BadRequest(new { error = "title_and_message_required" });

                if (dto.AppId <= 0)
                    return BadRequest(new { error = "valid_appId_required" });

                // Validate and categorize recipients
                var emails = new List<string>();
                var phones = new List<string>();
                var deviceIds = new List<string>();
                
                if (dto.Recipients != null && dto.Recipients.Length > 0)
                {
                    foreach (var r in dto.Recipients)
                    {
                        if (string.IsNullOrWhiteSpace(r.RecipientId)) continue;

                        var recipient = ValidateRecipient(r.RecipientId.Trim());
                        if (recipient != null)
                        {
                            switch (recipient.Type)
                            {
                                case RecipientType.Email:
                                    emails.Add(recipient.Id);
                                    break;
                                case RecipientType.WhatsApp:
                                    phones.Add(recipient.Id);
                                    break;
                                case RecipientType.Device:
                                    deviceIds.Add(recipient.Id);
                                    break;
                            }
                        }
                    }
                }

                _logger.LogInformation("[DEBUG] Validated {EmailCount} emails, {PhoneCount} phones, {DeviceCount} devices", 
                    emails.Count, phones.Count, deviceIds.Count);

                // Get alert type ID
                var alertTypeId = await _db.AlertType.AsNoTracking()
                    .Where(t => t.AlertTypeName == dto.AlertType)
                    .Select(t => t.AlertTypeId)
                    .FirstOrDefaultAsync();
                if (alertTypeId == 0)
                {
                    alertTypeId = await _db.AlertType.AsNoTracking()
                        .Select(t => t.AlertTypeId)
                        .FirstAsync();
                }

                // Use AlertSendService for robust sending
                var response = await _alertSendService.SendManualAsync(
                    dto.Title,
                    dto.Message,
                    emails,
                    phones,
                    emails.Count > 0, // sendEmail
                    phones.Count > 0, // sendWhatsApp
                    false, // sendDesktop (not implemented for API yet)
                    null, // userIds
                    alertTypeId
                );

                // Return 200 OK if all succeeded, 207 Multi-Status if partial success
                var statusCode = response.OverallSuccess ? 200 : 207;
                return StatusCode(statusCode, new 
                { 
                    alertId = response.AlerteId,
                    overallSuccess = response.OverallSuccess,
                    results = response.Results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private static ValidatedRecipient? ValidateRecipient(string recipientId)
        {
            // Email validation
            if (Regex.IsMatch(recipientId, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            {
                return new ValidatedRecipient { Id = recipientId, Type = RecipientType.Email };
            }

            // WhatsApp/Phone validation (international format)
            if (Regex.IsMatch(recipientId, @"^\+?[1-9]\d{7,14}$"))
            {
                return new ValidatedRecipient { Id = recipientId, Type = RecipientType.WhatsApp };
            }

            // Device ID validation (alphanumeric, 8-64 chars)
            if (Regex.IsMatch(recipientId, @"^[a-zA-Z0-9_-]{8,64}$"))
            {
                return new ValidatedRecipient { Id = recipientId, Type = RecipientType.Device };
            }

            return null;
        }

        private sealed class ValidatedRecipient
        {
            public string Id { get; set; } = string.Empty;
            public RecipientType Type { get; set; }
        }

        private enum RecipientType
        {
            Email,
            WhatsApp,
            Device
        }

        public sealed class MarkReadDto { public int DestinataireId { get; set; } }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id, [FromBody] MarkReadDto dto)
        {
            if (dto.DestinataireId <= 0) return BadRequest(new { error = "destinataireId_required" });
            var row = await _db.HistoriqueAlertes.FirstOrDefaultAsync(d => d.AlerteId == id && d.DestinataireId == dto.DestinataireId);
            if (row == null) return NotFound();
            row.EtatAlerteId = 2; // Lu
            row.DateLecture = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}


