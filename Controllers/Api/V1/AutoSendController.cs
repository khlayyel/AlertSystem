using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;
using AlertSystem.Services;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/alerts")]
    public class AutoSendController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AutoSendController> _logger;
        private readonly SmtpEmailSender _emailSender;
        private readonly IWhatsAppService _whatsAppService;

        public AutoSendController(
            ApplicationDbContext db, 
            ILogger<AutoSendController> logger,
            SmtpEmailSender emailSender,
            IWhatsAppService whatsAppService)
        {
            _db = db;
            _logger = logger;
            _emailSender = emailSender;
            _whatsAppService = whatsAppService;
        }

        /// <summary>
        /// Envoyer automatiquement une alerte par son ID (appelé par le trigger SQL)
        /// </summary>
        [HttpPost("send-by-id/{alerteId:int}")]
        public async Task<IActionResult> SendById(int alerteId)
        {
            try
            {
                _logger.LogInformation("AUTO-SEND: Début de l'envoi automatique pour l'alerte {AlerteId}", alerteId);

                // Récupérer l'alerte avec ses destinataires
                var alerte = await _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.HistoriqueAlertes)
                        .ThenInclude(h => h.User)
                    .FirstOrDefaultAsync(a => a.AlerteId == alerteId);

                if (alerte == null)
                {
                    _logger.LogWarning("AUTO-SEND: Alerte {AlerteId} non trouvée", alerteId);
                    return NotFound(new { error = "Alerte non trouvée" });
                }

                if (!alerte.HistoriqueAlertes.Any())
                {
                    _logger.LogWarning("AUTO-SEND: Aucun destinataire pour l'alerte {AlerteId}", alerteId);
                    return BadRequest(new { error = "Aucun destinataire trouvé" });
                }

                var results = new List<object>();
                var totalSent = 0;
                var totalErrors = 0;

                // Envoyer à chaque destinataire
                foreach (var historique in alerte.HistoriqueAlertes)
                {
                    var user = historique.User;
                    if (user == null) continue;

                    _logger.LogInformation("AUTO-SEND: Envoi à {UserName} ({Email})", user.FullName, user.Email);

                    var userResults = new List<string>();

                    // 1. Envoi Email
                    if (!string.IsNullOrEmpty(historique.DestinataireEmail))
                    {
                        try
                        {
                            await _emailSender.SendEmailAsync(
                                historique.DestinataireEmail,
                                alerte.TitreAlerte,
                                $"Alerte: {alerte.TitreAlerte}\n\n{alerte.DescriptionAlerte}\n\nType: {alerte.AlertType?.AlertTypeName}"
                            );
                            userResults.Add("Email: ✅ Envoyé");
                            totalSent++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "AUTO-SEND: Erreur email pour {Email}", historique.DestinataireEmail);
                            userResults.Add($"Email: ❌ Erreur - {ex.Message}");
                            totalErrors++;
                        }
                    }

                    // 2. Envoi WhatsApp
                    if (!string.IsNullOrEmpty(historique.DestinatairePhoneNumber))
                    {
                        try
                        {
                            var whatsappResult = await _whatsAppService.SendMessageAsync(
                                historique.DestinatairePhoneNumber,
                                $"🚨 *{alerte.TitreAlerte}*\n\n{alerte.DescriptionAlerte}"
                            );
                            
                            if (whatsappResult)
                            {
                                userResults.Add("WhatsApp: ✅ Envoyé");
                                totalSent++;
                            }
                            else
                            {
                                userResults.Add("WhatsApp: ❌ Échec de l'envoi");
                                totalErrors++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "AUTO-SEND: Erreur WhatsApp pour {Phone}", historique.DestinatairePhoneNumber);
                            userResults.Add($"WhatsApp: ❌ Erreur - {ex.Message}");
                            totalErrors++;
                        }
                    }

                    // 3. Web Push (si token disponible)
                    if (!string.IsNullOrEmpty(historique.DestinataireDesktop))
                    {
                        // TODO: Implémenter l'envoi Web Push si nécessaire
                        userResults.Add("WebPush: ⏳ Non implémenté");
                    }

                    results.Add(new
                    {
                        destinataireId = historique.DestinataireId,
                        user = new
                        {
                            fullName = user.FullName,
                            email = user.Email
                        },
                        results = userResults
                    });
                }

                _logger.LogInformation("AUTO-SEND: Terminé pour l'alerte {AlerteId} - {TotalSent} envoyés, {TotalErrors} erreurs", 
                    alerteId, totalSent, totalErrors);

                return Ok(new
                {
                    alerteId = alerteId,
                    titre = alerte.TitreAlerte,
                    totalDestinataires = alerte.HistoriqueAlertes.Count,
                    totalEnvoyes = totalSent,
                    totalErreurs = totalErrors,
                    details = results,
                    message = $"Envoi automatique terminé: {totalSent} succès, {totalErrors} erreurs"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AUTO-SEND: Erreur générale pour l'alerte {AlerteId}", alerteId);
                return StatusCode(500, new { 
                    error = "Erreur lors de l'envoi automatique", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Tester l'envoi automatique (endpoint de test)
        /// </summary>
        [HttpPost("test-auto-send")]
        public async Task<IActionResult> TestAutoSend()
        {
            try
            {
                // Créer une alerte de test
                var alerte = new Models.Entities.Alerte
                {
                    AlertTypeId = 2, // acquittementNonNécessaire
                    ExpedTypeId = 2, // Service
                    TitreAlerte = "Test Envoi Automatique",
                    DescriptionAlerte = "Ceci est un test d'envoi automatique déclenché par insertion SQL directe.",
                    DateCreationAlerte = DateTime.UtcNow,
                    StatutId = 1, // En Cours
                    EtatAlerteId = 2, // Non Lu
                    AppId = 1
                };

                _db.Alerte.Add(alerte);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Alerte de test créée avec succès",
                    alerteId = alerte.AlerteId,
                    info = "Le trigger SQL devrait automatiquement déclencher l'envoi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test d'envoi automatique");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
