using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;
using AlertSystem.Models.Entities;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HistoriqueAlerteController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HistoriqueAlerteController> _logger;

        public HistoriqueAlerteController(ApplicationDbContext db, ILogger<HistoriqueAlerteController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Obtenir l'historique complet des alertes avec destinataires
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHistorique(int page = 1, int size = 50)
        {
            try
            {
                var query = _db.HistoriqueAlertes
                    .Include(h => h.Alerte)
                    .Include(h => h.User)
                    .AsQueryable();

                var total = await query.CountAsync();
                
                var historique = await query
                    .OrderByDescending(h => h.Alerte!.DateCreationAlerte)
                    .ThenBy(h => h.DestinataireId)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(h => new
                    {
                        destinataireId = h.DestinataireId,
                        alerteId = h.AlerteId,
                        titreAlerte = h.Alerte!.TitreAlerte,
                        descriptionAlerte = h.Alerte.DescriptionAlerte,
                        dateCreation = h.Alerte.DateCreationAlerte,
                        destinataire = new
                        {
                            userId = h.DestinataireUserId,
                            fullName = h.User!.FullName,
                            email = h.User.Email,
                            phoneNumber = h.User.PhoneNumber
                        },
                        contact = new
                        {
                            email = h.DestinataireEmail,
                            phoneNumber = h.DestinatairePhoneNumber,
                            desktop = h.DestinataireDesktop
                        },
                        statut = new
                        {
                            etatAlerte = h.EtatAlerte,
                            dateLecture = h.DateLecture,
                            rappelSuivant = h.RappelSuivant
                        }
                    })
                    .ToListAsync();

                return Ok(new { 
                    items = historique, 
                    total, 
                    page, 
                    size,
                    totalPages = (int)Math.Ceiling((double)total / size)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'historique");
                return StatusCode(500, new { error = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Obtenir l'historique pour une alerte spécifique
        /// </summary>
        [HttpGet("alerte/{alerteId:int}")]
        public async Task<IActionResult> GetHistoriqueByAlerte(int alerteId)
        {
            try
            {
                var historique = await _db.HistoriqueAlertes
                    .Include(h => h.Alerte)
                    .Include(h => h.User)
                    .Where(h => h.AlerteId == alerteId)
                    .Select(h => new
                    {
                        destinataireId = h.DestinataireId,
                        alerteId = h.AlerteId,
                        destinataire = new
                        {
                            userId = h.DestinataireUserId,
                            fullName = h.User!.FullName,
                            email = h.User.Email,
                            phoneNumber = h.User.PhoneNumber
                        },
                        contact = new
                        {
                            email = h.DestinataireEmail,
                            phoneNumber = h.DestinatairePhoneNumber,
                            desktop = h.DestinataireDesktop
                        },
                        statut = new
                        {
                            etatAlerte = h.EtatAlerte,
                            dateLecture = h.DateLecture,
                            rappelSuivant = h.RappelSuivant
                        }
                    })
                    .ToListAsync();

                if (!historique.Any())
                {
                    return NotFound(new { error = "Aucun historique trouvé pour cette alerte" });
                }

                return Ok(historique);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'historique pour l'alerte {AlerteId}", alerteId);
                return StatusCode(500, new { error = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Obtenir les statistiques des alertes par destinataire
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStatistiques()
        {
            try
            {
                var stats = await _db.HistoriqueAlertes
                    .Include(h => h.User)
                    .Include(h => h.Alerte)
                    .GroupBy(h => new { h.DestinataireUserId, h.User!.FullName, h.User.Email })
                    .Select(g => new
                    {
                        destinataire = new
                        {
                            userId = g.Key.DestinataireUserId,
                            fullName = g.Key.FullName,
                            email = g.Key.Email
                        },
                        statistiques = new
                        {
                            totalAlertes = g.Count(),
                            alertesLues = g.Count(h => h.EtatAlerte == "Lu"),
                            alertesNonLues = g.Count(h => h.EtatAlerte == "Non Lu"),
                            rappelsEnAttente = g.Count(h => h.RappelSuivant != null && h.RappelSuivant > DateTime.UtcNow),
                            tauxLecture = g.Count() > 0 ? (double)g.Count(h => h.EtatAlerte == "Lu") / g.Count() * 100 : 0
                        }
                    })
                    .OrderByDescending(s => s.statistiques.totalAlertes)
                    .ToListAsync();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques");
                return StatusCode(500, new { error = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Marquer une alerte comme lue pour un destinataire
        /// </summary>
        [HttpPost("{destinataireId:int}/marquer-lu")]
        public async Task<IActionResult> MarquerCommeLu(int destinataireId)
        {
            try
            {
                var historique = await _db.HistoriqueAlertes
                    .FirstOrDefaultAsync(h => h.DestinataireId == destinataireId);

                if (historique == null)
                {
                    return NotFound(new { error = "Destinataire non trouvé" });
                }

                if (historique.EtatAlerte == "Lu")
                {
                    return BadRequest(new { error = "Cette alerte est déjà marquée comme lue" });
                }

                historique.EtatAlerte = "Lu";
                historique.DateLecture = DateTime.UtcNow;
                
                await _db.SaveChangesAsync();

                return Ok(new { 
                    message = "Alerte marquée comme lue",
                    destinataireId = historique.DestinataireId,
                    dateLecture = historique.DateLecture
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage comme lu pour le destinataire {DestinataireId}", destinataireId);
                return StatusCode(500, new { error = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Obtenir les rappels en attente
        /// </summary>
        [HttpGet("rappels")]
        public async Task<IActionResult> GetRappelsEnAttente()
        {
            try
            {
                var rappels = await _db.HistoriqueAlertes
                    .Include(h => h.Alerte)
                    .Include(h => h.User)
                    .Where(h => h.RappelSuivant != null && 
                               h.RappelSuivant > DateTime.UtcNow && 
                               h.EtatAlerte == "Non Lu")
                    .OrderBy(h => h.RappelSuivant)
                    .Select(h => new
                    {
                        destinataireId = h.DestinataireId,
                        alerteId = h.AlerteId,
                        titreAlerte = h.Alerte!.TitreAlerte,
                        destinataire = new
                        {
                            fullName = h.User!.FullName,
                            email = h.User.Email
                        },
                        rappelSuivant = h.RappelSuivant,
                        minutesRestantes = EF.Functions.DateDiffMinute(DateTime.UtcNow, h.RappelSuivant!.Value)
                    })
                    .ToListAsync();

                return Ok(rappels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des rappels");
                return StatusCode(500, new { error = "Erreur interne du serveur" });
            }
        }
    }
}
