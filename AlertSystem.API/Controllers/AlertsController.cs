using Microsoft.AspNetCore.Mvc;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Services;
using System.Text.RegularExpressions;
using AlertSystem.Entities.Entities;
using AlertSystem.Service.Interfaces;
using AlertSystem.Service.Services;

namespace AlertSystem.Controllers.Api.V1
{
    /// <summary>
    /// Contrôleur refactorisé pour la gestion des alertes avec la nouvelle structure
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public sealed class AlertsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly AlertSendService _alertSendService;
        private readonly AlertReadService _alertReadService;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(
            ApplicationDbContext db, 
            INotificationService notificationService, 
            AlertSendService alertSendService,
            AlertReadService alertReadService,
            ILogger<AlertsController> logger)
        { 
            _db = db; 
            _notificationService = notificationService;
            _alertSendService = alertSendService;
            _alertReadService = alertReadService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère une alerte par son ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var alert = await _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.Statut)
                    .Include(a => a.Etat)
                    .Include(a => a.PlateformeEnvoie)
                    .Include(a => a.DestinataireUser)
                    .AsNoTracking()
                    .Where(x => x.AlertRecordId == id)
                    .Select(a => new
                    {
                        alertRecordId = a.AlertRecordId,
                        alertGroupId = a.AlertGroupId,
                        title = a.TitreAlerte,
                        description = a.DescriptionAlerte,
                        alertTypeId = a.AlertTypeId,
                        statutId = a.StatutId,
                        etatAlerteId = a.EtatAlerteId,
                        dateCreation = a.DateCreationAlerte,
                        appId = a.AppId,
                        expediteurId = a.ExpediteurId,
                        plateformeEnvoieId = a.PlateformeEnvoieId,
                        destinataireutil_id = a.DestinataireUserId,
                        destinataireEmail = a.DestinataireEmail,
                        destinatairePhoneNumber = a.DestinatairePhoneNumber,
                        destinataireDesktop = a.DestinataireDesktop,
                        dateLecture = a.DateLecture,
                        rappelSuivant = a.RappelSuivant,
                        processedByWorker = a.ProcessedByWorker,
                        alertType = a.AlertType != null ? new { id = a.AlertType.AlertTypeId, name = a.AlertType.AlertTypeName } : null,
                        statut = a.Statut != null ? new { id = a.Statut.StatutId, name = a.Statut.StatutName } : null,
                        etat = a.Etat != null ? new { id = a.Etat.EtatAlerteId, name = a.Etat.EtatAlerteName } : null,
                        plateformeEnvoie = a.PlateformeEnvoie != null ? new { id = a.PlateformeEnvoie.PlateformeId, name = a.PlateformeEnvoie.Plateforme } : null,
                        destinataireUser = a.DestinataireUser != null ? new { id = a.DestinataireUser.util_id, name = a.DestinataireUser.util_nom } : null
                    })
                    .FirstOrDefaultAsync();

                if (alert == null)
                    return NotFound($"Alert with ID {id} not found");

                return Ok(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alert {AlertId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Récupère les alertes avec filtres et pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Query(
            [FromQuery] int? alertTypeId = null,
            [FromQuery] int? statutId = null,
            [FromQuery] int? plateformeEnvoieId = null,
            [FromQuery] int? appId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.Statut)
                    .Include(a => a.Etat)
                    .Include(a => a.PlateformeEnvoie)
                    .Include(a => a.DestinataireUser)
                    .AsNoTracking();

                // Apply filters
                if (alertTypeId.HasValue)
                    query = query.Where(a => a.AlertTypeId == alertTypeId.Value);

                if (statutId.HasValue)
                    query = query.Where(a => a.StatutId == statutId.Value);

                if (plateformeEnvoieId.HasValue)
                    query = query.Where(a => a.PlateformeEnvoieId == plateformeEnvoieId.Value);
                
                if (appId.HasValue)
                    query = query.Where(a => a.AppId == appId.Value);

                if (startDate.HasValue)
                    query = query.Where(a => a.DateCreationAlerte >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.DateCreationAlerte <= endDate.Value);

                var totalCount = await query.CountAsync();

                var alerts = await query
                    .OrderByDescending(a => a.DateCreationAlerte)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        alertRecordId = a.AlertRecordId,
                        alertGroupId = a.AlertGroupId,
                        title = a.TitreAlerte,
                        description = a.DescriptionAlerte,
                        alertTypeId = a.AlertTypeId,
                        statutId = a.StatutId,
                        etatAlerteId = a.EtatAlerteId,
                        dateCreation = a.DateCreationAlerte,
                        appId = a.AppId,
                        expediteurId = a.ExpediteurId,
                        plateformeEnvoieId = a.PlateformeEnvoieId,
                        destinataireutil_id = a.DestinataireUserId,
                        destinataireEmail = a.DestinataireEmail,
                        destinatairePhoneNumber = a.DestinatairePhoneNumber,
                        destinataireDesktop = a.DestinataireDesktop,
                        dateLecture = a.DateLecture,
                        rappelSuivant = a.RappelSuivant,
                        processedByWorker = a.ProcessedByWorker,
                        alertType = a.AlertType != null ? new { id = a.AlertType.AlertTypeId, name = a.AlertType.AlertTypeName } : null,
                        statut = a.Statut != null ? new { id = a.Statut.StatutId, name = a.Statut.StatutName } : null,
                        etat = a.Etat != null ? new { id = a.Etat.EtatAlerteId, name = a.Etat.EtatAlerteName } : null,
                        plateformeEnvoie = a.PlateformeEnvoie != null ? new { id = a.PlateformeEnvoie.PlateformeId, name = a.PlateformeEnvoie.Plateforme } : null,
                        destinataireUser = a.DestinataireUser != null ? new { id = a.DestinataireUser.util_id, name = a.DestinataireUser.util_nom } : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    data = alerts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying alerts");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Crée une nouvelle alerte (refactorisé pour un seul destinataire/plateforme)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAlertDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate recipient based on platform
                if (!ValidateRecipientForPlatform(dto.Recipient, dto.PlateformeEnvoieId))
                    return BadRequest($"Invalid recipient format for platform {dto.PlateformeEnvoieId}");

                // Get API client info
                var apiClientId = HttpContext.Items["ApiClientId"] as int?;
                var apiClientName = HttpContext.Items["ApiClientName"] as string;

                _logger.LogInformation("Creating alert: {Title} to {Recipient} via platform {PlatformId} from client {ClientId}", 
                    dto.Title, dto.Recipient, dto.PlateformeEnvoieId, apiClientId);

                // Create alert record for WatcherWorker to process
                var alertGroupId = Guid.NewGuid();
                var alertRecord = new AlertSystem.Entities.Entities.Alerte
                {
                    AlertGroupId = alertGroupId,
                    TitreAlerte = dto.Title,
                    DescriptionAlerte = dto.Message,
                    DateCreationAlerte = DateTime.UtcNow,
                    StatutId = 1, // En Cours (pending)
                    AlertTypeId = dto.AlertTypeId,
                    EtatAlerteId = 1, // Non Lu
                    PlateformeEnvoieId = dto.PlateformeEnvoieId,
                    DestinataireEmail = dto.PlateformeEnvoieId == 1 ? dto.Recipient : null,
                    DestinatairePhoneNumber = dto.PlateformeEnvoieId == 2 ? dto.Recipient : null,
                    DestinataireUserId = dto.PlateformeEnvoieId == 3 ? int.Parse(dto.Recipient) : null,
                    ExpediteurId = dto.ExpediteurId,
                    ProcessedByWorker = false // Let WatcherWorker process this
                };

                _db.Alerte.Add(alertRecord);
                await _db.SaveChangesAsync();

                var result = new { Success = true, AlertGroupId = alertGroupId, AlertRecordId = alertRecord.AlertRecordId };

                return Ok(new
                {
                    success = true,
                    alertGroupId = result.AlertGroupId,
                    alertRecordId = result.AlertRecordId,
                    message = "Alert queued for processing by WatcherWorker"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Marque une alerte comme lue
        /// </summary>
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(int id, [FromBody] MarkReadDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var success = await _alertReadService.MarkAsReadAsync(id);
                
                if (success)
                {
                    return Ok(new { success = true, message = "Alert marked as read" });
                }
                else
                {
                    return NotFound($"Alert with ID {id} not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking alert {AlertId} as read", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Récupère les statistiques d'alertes
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var statistics = await _alertReadService.GetStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alert statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Valide le format du destinataire selon la plateforme
        /// </summary>
        private bool ValidateRecipientForPlatform(string recipient, int plateformeEnvoieId)
        {
            return plateformeEnvoieId switch
            {
                1 => ValidateEmail(recipient), // Email
                2 => ValidatePhone(recipient), // WhatsApp
                3 => Validateutil_id(recipient), // Desktop
                _ => false
            };
        }

        /// <summary>
        /// Valide le format d'email
        /// </summary>
        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }

        /// <summary>
        /// Valide le format de numéro de téléphone
        /// </summary>
        private bool ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var phoneRegex = new Regex(@"^\+?[1-9]\d{1,14}$");
            return phoneRegex.IsMatch(phone);
        }

        /// <summary>
        /// Valide le format d'ID utilisateur
        /// </summary>
        private bool Validateutil_id(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;
            return int.TryParse(userId, out _);
        }

        #endregion
    }

    #region DTOs

    /// <summary>
    /// DTO pour la création d'alerte (refactorisé pour un seul destinataire/plateforme)
    /// </summary>
    public class CreateAlertDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public int PlateformeEnvoieId { get; set; }
        public int AlertTypeId { get; set; }
        public int? ExpediteurId { get; set; }
    }

    /// <summary>
    /// DTO pour marquer une alerte comme lue
    /// </summary>
    public class MarkReadDto
    {
        public int DestinataireId { get; set; }
    }

    #endregion
}
