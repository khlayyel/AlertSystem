using AlertSystem.Data;
using AlertSystem.Service;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.WEB.Services
{
    public interface IDelayedAlertJobService
    {
        Task ExecuteDelayedSendAsync(int alerteId);
    }

    public class DelayedAlertJobService : IDelayedAlertJobService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAlertSendService _alertSendService;
        private readonly ILogger<DelayedAlertJobService> _logger;

        public DelayedAlertJobService(
            ApplicationDbContext db,
            IAlertSendService alertSendService,
            ILogger<DelayedAlertJobService> logger)
        {
            _db = db;
            _alertSendService = alertSendService;
            _logger = logger;
        }

        public async Task ExecuteDelayedSendAsync(int alerteId)
        {
            try
            {
                _logger.LogInformation("Executing delayed send for alert {AlerteId}", alerteId);

                // Get the alert from database
                var alert = await _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.Statut)
                    .FirstOrDefaultAsync(a => a.AlerteId == alerteId);

                if (alert == null)
                {
                    _logger.LogWarning("Alert {AlerteId} not found for delayed send", alerteId);
                    return;
                }

                // Check if alert was cancelled
                if (alert.StatutId == 3) // Annulé
                {
                    _logger.LogInformation("Alert {AlerteId} was cancelled, skipping send", alerteId);
                    return;
                }

                // Get recipients from HistoriqueAlerte
                var historique = await _db.HistoriqueAlertes
                    .Where(h => h.AlerteId == alerteId)
                    .ToListAsync();

                var emails = historique
                    .Where(h => !string.IsNullOrEmpty(h.DestinataireEmail))
                    .Select(h => h.DestinataireEmail!)
                    .ToList();

                var phones = historique
                    .Where(h => !string.IsNullOrEmpty(h.DestinatairePhoneNumber))
                    .Select(h => h.DestinatairePhoneNumber!)
                    .ToList();

                // Determine platforms to use
                var sendEmail = emails.Any();
                var sendWhatsApp = phones.Any();
                var sendDesktop = historique.Any(h => !string.IsNullOrEmpty(h.DestinataireDesktop));

                // Execute the actual send
                var response = await _alertSendService.SendManualAsync(
                    alert.TitreAlerte,
                    alert.DescriptionAlerte ?? "",
                    emails,
                    phones,
                    sendEmail,
                    sendWhatsApp,
                    sendDesktop,
                    null, // userIds
                    alert.AlertTypeId
                );

                _logger.LogInformation("Delayed send completed for alert {AlerteId}. Success: {Success}", 
                    alerteId, response.OverallSuccess);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing delayed send for alert {AlerteId}", alerteId);
                
                // Update alert status to failed
                var alert = await _db.Alerte.FindAsync(alerteId);
                if (alert != null)
                {
                    alert.StatutId = 4; // Échoué
                    await _db.SaveChangesAsync();
                }
            }
        }
    }
}
