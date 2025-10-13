using Microsoft.AspNetCore.Mvc;
using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public sealed class SeedController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SeedController> _logger;

        public SeedController(ApplicationDbContext db, ILogger<SeedController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("database")]
        public async Task<IActionResult> SeedDatabase()
        {
            try
            {
                _logger.LogInformation("Starting database seeding...");

                // Ensure database exists
                await _db.Database.EnsureCreatedAsync();

                var seededItems = new List<string>();

                // Seed AlertType
                if (!await _db.AlertType.AnyAsync())
                {
                    _db.AlertType.AddRange(
                        new AlertType { AlertTypeName = "acquittementNécessaire" },
                        new AlertType { AlertTypeName = "acquittementNonNécessaire" }
                    );
                    seededItems.Add("AlertType");
                }

                // Seed ExpedType
                if (!await _db.ExpedType.AnyAsync())
                {
                    _db.ExpedType.AddRange(
                        new ExpedType { ExpedTypeName = "Humain" },
                        new ExpedType { ExpedTypeName = "Service" }
                    );
                    seededItems.Add("ExpedType");
                }

                // Seed Statut
                if (!await _db.Statut.AnyAsync())
                {
                    _db.Statut.AddRange(
                        new Statut { StatutName = "En Cours" },
                        new Statut { StatutName = "Terminé" },
                        new Statut { StatutName = "Échoué" }
                    );
                    seededItems.Add("Statut");
                }

                // Seed Etat
                if (!await _db.Etat.AnyAsync())
                {
                    _db.Etat.AddRange(
                        new Etat { EtatAlerteName = "Non Lu" },
                        new Etat { EtatAlerteName = "Lu" }
                    );
                    seededItems.Add("Etat");
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Database seeding completed successfully. Seeded: {Items}", string.Join(", ", seededItems));

                return Ok(new 
                { 
                    message = "Database seeded successfully",
                    seededTables = seededItems,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                return StatusCode(500, new { error = "Database seeding failed", details = ex.Message });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetSeedStatus()
        {
            try
            {
                var status = new
                {
                    databaseExists = await _db.Database.CanConnectAsync(),
                    alertTypeCount = await _db.AlertType.CountAsync(),
                    expedTypeCount = await _db.ExpedType.CountAsync(),
                    statutCount = await _db.Statut.CountAsync(),
                    etatCount = await _db.Etat.CountAsync(),
                    apiClientCount = await _db.ApiClients.CountAsync(),
                    alerteCount = await _db.Alerte.CountAsync()
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Could not check database status", details = ex.Message });
            }
        }
    }
}
