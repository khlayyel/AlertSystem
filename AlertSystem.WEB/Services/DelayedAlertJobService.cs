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
        private readonly IKpiUpdateService _kpiUpdateService;

        public DelayedAlertJobService(
            ApplicationDbContext db,
            IAlertSendService alertSendService,
            ILogger<DelayedAlertJobService> logger,
            IKpiUpdateService kpiUpdateService)
        {
            _db = db;
            _alertSendService = alertSendService;
            _logger = logger;
            _kpiUpdateService = kpiUpdateService;
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

                // Execute the actual send using existing alert (don't create new one)
                var response = await _alertSendService.SendExistingAlertAsync(
                    alert.AlerteId,
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

                // Send real-time KPI update after alert is sent
                try
                {
                    // For background jobs, we'll send updates to all connected users
                    // In a real scenario, you might want to determine which users should receive updates
                    await _kpiUpdateService.SendOutboxKpiUpdateAsync(1); // Default user for now
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send outbox KPI update after delayed send");
                }

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
