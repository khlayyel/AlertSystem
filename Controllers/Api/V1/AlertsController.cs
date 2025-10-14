using Microsoft.AspNetCore.Mvc;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Services;
using System.Text.RegularExpressions;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public sealed class AlertsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(ApplicationDbContext db, INotificationService notificationService, ILogger<AlertsController> logger)
        { 
            _db = db; 
            _notificationService = notificationService;
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
                            etatAlerte = d.EtatAlerte,
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
            catch (Exception ex)
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
            catch (Exception ex)
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
                var validatedRecipients = new List<ValidatedRecipient>();
                if (dto.Recipients != null && dto.Recipients.Length > 0)
                {
                    foreach (var r in dto.Recipients)
                    {
                        if (string.IsNullOrWhiteSpace(r.RecipientId)) continue;

                        var recipient = ValidateRecipient(r.RecipientId.Trim());
                        if (recipient != null)
                        {
                            validatedRecipients.Add(recipient);
                        }
                    }
                }

                _logger.LogInformation("[DEBUG] Validated {Count} recipients", validatedRecipients.Count);

                // Get reference IDs
                var expedTypeId = await _db.ExpedType.AsNoTracking()
                    .Where(x => x.ExpedTypeName == dto.ExpedType)
                    .Select(x => x.ExpedTypeId)
                    .FirstOrDefaultAsync();
                if (expedTypeId == 0)
                {
                    expedTypeId = await _db.ExpedType.AsNoTracking()
                        .Where(x => x.ExpedTypeName == "Service")
                        .Select(x => x.ExpedTypeId)
                        .FirstAsync();
                }

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

                var statutId = await _db.Statut.AsNoTracking()
                    .Where(s => s.StatutName == "En Cours")
                    .Select(s => s.StatutId)
                    .FirstAsync();

                var etatId = await _db.Etat.AsNoTracking()
                    .Where(e => e.EtatAlerteName == "Non Lu")
                    .Select(e => e.EtatAlerteId)
                    .FirstAsync();

                // Create alert
                var alert = new Models.Entities.Alerte
                {
                    TitreAlerte = dto.Title,
                    DescriptionAlerte = dto.Message,
                    AlertTypeId = alertTypeId,
                    AppId = dto.AppId,
                    ExpedTypeId = expedTypeId,
                    ExpediteurId = dto.ExpediteurId,
                    DateCreationAlerte = DateTime.UtcNow,
                    StatutId = statutId,
                    EtatAlerteId = etatId
                };

                _db.Alerte.Add(alert);
                await _db.SaveChangesAsync();

                _logger.LogInformation("[DEBUG] Alert created with ID: {AlertId}", alert.AlerteId);

                // Create single recipient record per alert
                var historiqueAlerte = new Models.Entities.HistoriqueAlerte
                {
                    AlerteId = alert.AlerteId,
                    DestinataireUserId = 1, // TODO: Utiliser le vrai UserId du destinataire
                    EtatAlerte = "Non Lu",
                    DestinataireEmail = "test@example.com", // TODO: Récupérer depuis Users ou Recipients
                    DestinatairePhoneNumber = "+21699414008", // TODO: Récupérer depuis Users ou Recipients
                    DestinataireDesktop = "desktop-token" // TODO: Récupérer depuis Users ou Recipients
                };
                _db.HistoriqueAlertes.Add(historiqueAlerte);
                await _db.SaveChangesAsync();

                // Send notifications
                var notificationResults = new List<string>();
                foreach (var recipient in validatedRecipients)
                {
                    try
                    {
                        bool success = false;
                        switch (recipient.Type)
                        {
                            case RecipientType.Email:
                                success = await _notificationService.SendEmailAsync(recipient.Id, dto.Title, dto.Message);
                                if (success) notificationResults.Add($"Email sent to {recipient.Id}");
                                break;
                            case RecipientType.WhatsApp:
                                success = await _notificationService.SendWhatsAppAsync(recipient.Id, $"🚨 {dto.Title}\n\n{dto.Message}");
                                if (success) notificationResults.Add($"WhatsApp sent to {recipient.Id}");
                                break;
                            case RecipientType.Device:
                                // For now, log as desktop notification (implement WebPush later)
                                _logger.LogInformation("Desktop notification would be sent to device: {DeviceId}", recipient.Id);
                                notificationResults.Add($"Desktop notification queued for {recipient.Id}");
                                break;
                        }

                        if (!success)
                        {
                            _logger.LogWarning("Failed to send {Type} notification to {Recipient}", recipient.Type, recipient.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending notification to {Recipient}", recipient.Id);
                    }
                }

                return CreatedAtAction(nameof(GetById), new { id = alert.AlerteId }, new 
                { 
                    alertId = alert.AlerteId,
                    recipientsCreated = 1, // Un seul destinataire par alerte
                    notificationsSent = notificationResults.Count,
                    notifications = notificationResults
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
            row.EtatAlerte = "Lu";
            row.DateLecture = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}


