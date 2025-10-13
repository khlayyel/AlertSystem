using Microsoft.AspNetCore.Mvc;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Controllers
{
    public sealed class AlertsCrudController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AlertsCrudController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> QuickList()
        {
            try
            {
                // Return some predefined quick alert templates
                var quickAlerts = new[]
                {
                    new { id = 1, title = "Réunion d'urgence", message = "Réunion d'urgence dans 15 minutes en salle de conférence", type = "Obligatoire" },
                    new { id = 2, title = "Maintenance système", message = "Maintenance système prévue ce soir de 20h à 22h", type = "Information" },
                    new { id = 3, title = "Alerte sécurité", message = "Veuillez évacuer le bâtiment immédiatement", type = "Obligatoire" },
                    new { id = 4, title = "Pause déjeuner", message = "La cafétéria sera fermée aujourd'hui pour maintenance", type = "Information" },
                    new { id = 5, title = "Formation obligatoire", message = "Formation sécurité obligatoire demain à 14h", type = "Obligatoire" }
                };

                return Json(quickAlerts);
            }
            catch (Exception ex)
            {
                return Json(new object[0]);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateFromTemplate([FromBody] CreateFromTemplateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Message))
                {
                    return BadRequest(new { error = "Title and message are required" });
                }

                // Get default IDs for the alert
                var alertTypeId = await _db.AlertType
                    .Where(t => t.AlertTypeName == dto.Type)
                    .Select(t => t.AlertTypeId)
                    .FirstOrDefaultAsync();

                if (alertTypeId == 0)
                {
                    alertTypeId = await _db.AlertType.Select(t => t.AlertTypeId).FirstAsync();
                }

                var expedTypeId = await _db.ExpedType
                    .Where(e => e.ExpedTypeName == "Service")
                    .Select(e => e.ExpedTypeId)
                    .FirstAsync();

                var statutId = await _db.Statut
                    .Where(s => s.StatutName == "En Cours")
                    .Select(s => s.StatutId)
                    .FirstAsync();

                var etatId = await _db.Etat
                    .Where(e => e.EtatAlerteName == "Non Lu")
                    .Select(e => e.EtatAlerteId)
                    .FirstAsync();

                var alert = new Models.Entities.Alerte
                {
                    TitreAlerte = dto.Title,
                    DescriptionAlerte = dto.Message,
                    AlertTypeId = alertTypeId,
                    ExpedTypeId = expedTypeId,
                    StatutId = statutId,
                    EtatAlerteId = etatId,
                    DateCreationAlerte = DateTime.UtcNow,
                    AppId = 1 // Default app ID
                };

                _db.Alerte.Add(alert);
                await _db.SaveChangesAsync();

                return Json(new { success = true, alertId = alert.AlerteId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Failed to create alert", details = ex.Message });
            }
        }

        public sealed class CreateFromTemplateDto
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Type { get; set; } = "Information";
        }
    }
}
