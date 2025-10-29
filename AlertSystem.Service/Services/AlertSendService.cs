using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using AlertSystem.Service.Interfaces;
using AlertSystem.Services;
using AlertSystem.Utils.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace AlertSystem.Service.Services
{
    /// <summary>
    /// Service principal pour l'envoi d'alertes - respecte le principe SRP
    /// Responsabilité unique : Orchestrer l'envoi d'alertes
    /// </summary>
    public class AlertSendService : IAlertSendService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AlertSendService> _logger;
        private readonly IConfiguration _cfg;
        private readonly INotificationService _notificationService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IWhatsAppTemplateService _whatsAppTemplateService;
        private readonly ConfirmationTokenService _confirmationTokenService;
        private readonly IKpiUpdateService _kpiUpdateService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AlertSendService(
            ApplicationDbContext db,
            ILogger<AlertSendService> logger,
            IConfiguration cfg,
            INotificationService notificationService,
            IWhatsAppService whatsAppService,
            IEmailTemplateService emailTemplateService,
            IWhatsAppTemplateService whatsAppTemplateService,
            ConfirmationTokenService confirmationTokenService,
            IKpiUpdateService kpiUpdateService,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _logger = logger;
            _cfg = cfg;
            _notificationService = notificationService;
            _whatsAppService = whatsAppService;
            _emailTemplateService = emailTemplateService;
            _whatsAppTemplateService = whatsAppTemplateService;
            _confirmationTokenService = confirmationTokenService;
            _kpiUpdateService = kpiUpdateService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Envoie une alerte existante par son AlertRecordId
        /// </summary>
        public async Task<bool> SendExistingAlertAsync(int alertRecordId, string[] emails, string[] phones, string[] desktop)
        {
            try
            {
                var alert = await _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.Statut)
                    .Include(a => a.Etat)
                    .Include(a => a.PlateformeEnvoie)
                    .FirstOrDefaultAsync(a => a.AlertRecordId == alertRecordId);

                if (alert == null)
                {
                    _logger.LogWarning("Alert with ID {AlertRecordId} not found", alertRecordId);
                    return false;
                }

                _logger.LogInformation("Sending alert {AlertRecordId} with title: {Title} via platform {Platform}", alertRecordId, alert.TitreAlerte, alert.PlateformeEnvoieId);

                bool success = false;

                // Envoyer selon la plateforme
                if (alert.PlateformeEnvoieId == 1) // Email
                {
                    if (!string.IsNullOrEmpty(alert.DestinataireEmail))
                    {
                        // Build sender display with real expediteur info
                        string senderDisplay = "AlertSystem";
                        if (alert.ExpediteurId.HasValue)
                        {
                            var sender = await _db.DefUtilisateurs
                                .FirstOrDefaultAsync(u => u.util_id == alert.ExpediteurId.Value);
                            if (sender != null)
                            {
                                var prenom = string.IsNullOrWhiteSpace(sender.util_prenom) ? string.Empty : sender.util_prenom.Trim();
                                var nom = string.IsNullOrWhiteSpace(sender.util_nom) ? string.Empty : sender.util_nom.Trim();
                                var email = string.IsNullOrWhiteSpace(sender.util_email) ? string.Empty : sender.util_email.Trim();
                                var fullName = (prenom + " " + nom).Trim();
                                senderDisplay = string.IsNullOrWhiteSpace(email)
                                    ? fullName
                                    : string.IsNullOrWhiteSpace(fullName) ? email : $"{fullName} <{email}>";
                            }
                        }

                        // Generate confirmation token URL for email
                        var token = _confirmationTokenService.Generate(new ConfirmPayload
                        {
                            AlerteId = alert.AlertRecordId,
                            Kind = "email",
                            Value = alert.DestinataireEmail
                        });
                        var req = _httpContextAccessor.HttpContext?.Request;
                        var scheme = req?.Scheme ?? (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ? "http" : "https");
                        var host = req != null ? req.Host.ToString() : (_cfg["App:PublicHost"] ?? "localhost:5185");
                        var confirmationUrl = $"{scheme}://{host}/confirm?t={token}";

                        var confirmLabel = (alert.AlertTypeId == 2) ? "✅ Confirmer la réception" : "👁️ Marquer comme lu";
                        var htmlContent = _emailTemplateService.CreateAlertEmailTemplate(
                            alert.TitreAlerte,
                            alert.DescriptionAlerte,
                            senderDisplay,
                            DateTime.Now,
                            confirmationUrl,
                            confirmLabel);
                        success = await _notificationService.SendHtmlEmailAsync(alert.DestinataireEmail, alert.TitreAlerte, htmlContent);
                    }
                    else
                    {
                        _logger.LogWarning("No email recipient for alert {AlertRecordId}", alertRecordId);
                        success = false;
                    }
                }
                else if (alert.PlateformeEnvoieId == 2) // WhatsApp
                {
                    if (!string.IsNullOrEmpty(alert.DestinatairePhoneNumber))
                    {
                        // Build sender display with real expediteur info
                        string senderDisplay = "AlertSystem";
                        if (alert.ExpediteurId.HasValue)
                        {
                            var sender = await _db.DefUtilisateurs
                                .FirstOrDefaultAsync(u => u.util_id == alert.ExpediteurId.Value);
                            if (sender != null)
                            {
                                var prenom = string.IsNullOrWhiteSpace(sender.util_prenom) ? string.Empty : sender.util_prenom.Trim();
                                var nom = string.IsNullOrWhiteSpace(sender.util_nom) ? string.Empty : sender.util_nom.Trim();
                                var email = string.IsNullOrWhiteSpace(sender.util_email) ? string.Empty : sender.util_email.Trim();
                                var fullName = (prenom + " " + nom).Trim();
                                senderDisplay = string.IsNullOrWhiteSpace(email)
                                    ? fullName
                                    : string.IsNullOrWhiteSpace(fullName) ? email : $"{fullName} <{email}>";
                            }
                        }
                        // Use IWhatsAppTemplateService to send with approved Meta template
                        // Generate confirmation URL with signed token for WhatsApp
                        var tokenWa = _confirmationTokenService.Generate(new ConfirmPayload
                        {
                            AlerteId = alert.AlertRecordId,
                            Kind = "wa",
                            Value = alert.DestinatairePhoneNumber
                        });
                        var reqWa = _httpContextAccessor.HttpContext?.Request;
                        var schemeWa = reqWa?.Scheme ?? (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ? "http" : "https");
                        var hostWa = reqWa != null ? reqWa.Host.ToString() : (_cfg["App:PublicHost"] ?? "localhost:5185");
                        var confirmationUrl = $"{schemeWa}://{hostWa}/confirm?t={tokenWa}";
                        success = await _whatsAppTemplateService.SendAlertTemplateAsync(
                            alert.DestinatairePhoneNumber, 
                            alert.TitreAlerte, 
                            alert.DescriptionAlerte, 
                            senderDisplay,
                            confirmationUrl,
                            alert.AlertTypeId == 2);
                    }
                    else
                    {
                        _logger.LogWarning("No WhatsApp phone number for alert {AlertRecordId}", alertRecordId);
                        success = false;
                    }
                }
                else if (alert.PlateformeEnvoieId == 3) // Desktop WebPush
                {
                    try
                    {
                        var title = string.IsNullOrWhiteSpace(alert.TitreAlerte) ? "Alerte" : alert.TitreAlerte;
                        var body = alert.DescriptionAlerte ?? string.Empty;
                        var url = "/Dashboard";
                        if (alert.DestinataireUserId.HasValue)
                        {
                            var uid = Convert.ToInt32(alert.DestinataireUserId.Value);
                            success = await _notificationService.SendPushNotificationAsync(uid, title, body, url);
                        }
                        else
                        {
                            _logger.LogWarning("No DestinataireUserId for desktop push on alert {AlertRecordId}", alertRecordId);
                            success = false;
                        }
                    }
                    catch (Exception pushEx)
                    {
                        _logger.LogError(pushEx, "Error sending web push for alert {AlertRecordId}", alertRecordId);
                        success = false;
                    }
                }
                else
                {
                    _logger.LogWarning("Unknown platform {PlatformId} for alert {AlertRecordId}", alert.PlateformeEnvoieId, alertRecordId);
                    success = false;
                }

                // Mettre à jour le statut
                if (success)
                {
                    alert.StatutId = 2; // Envoyé
                    alert.DateLecture = DateTime.UtcNow;
                    _logger.LogInformation("Alert {AlertRecordId} sent successfully", alertRecordId);
                }
                else
                {
                    _logger.LogError("Alert {AlertRecordId} failed to send", alertRecordId);
                }

                // Gérer les rappels pour les alertes obligatoires
                if (alert.AlertTypeId == 2) // Obligatoire (acquittementNecessaire)
                {
                    alert.RappelSuivant = DateTime.UtcNow.AddHours(24);
                }

                await _db.SaveChangesAsync();
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending alert {AlertRecordId}", alertRecordId);
                
                // Marquer comme échoué
                var alert = await _db.Alerte.FindAsync(alertRecordId);
                if (alert != null)
                {
                    alert.StatutId = 4; // Échoué
                    await _db.SaveChangesAsync();
                }
                
                return false;
            }
        }

        /// <summary>
        /// Envoie une nouvelle alerte
        /// </summary>
        public async Task<bool> SendAlertAsync(string title, string description, int alertTypeId, string[] emails, string[] phones, string[] desktop)
        {
            try
            {
                var alertGroupId = Guid.NewGuid();
                var alerts = new List<Alerte>();

                // Créer une alerte pour chaque destinataire/plateforme
                foreach (var email in emails)
                {
                    alerts.Add(CreateAlertRecord(alertGroupId, title, description, alertTypeId, 1, email, null, null));
                }

                foreach (var phone in phones)
                {
                    alerts.Add(CreateAlertRecord(alertGroupId, title, description, alertTypeId, 2, null, phone, null));
                }

                foreach (var desktopUser in desktop)
                {
                    alerts.Add(CreateAlertRecord(alertGroupId, title, description, alertTypeId, 3, null, null, desktopUser));
                }

                _db.Alerte.AddRange(alerts);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Created {Count} alert records for group {AlertGroupId}", alerts.Count, alertGroupId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert records");
                return false;
            }
        }

        private Alerte CreateAlertRecord(Guid alertGroupId, string title, string description, int alertTypeId, int plateformeId, string? email, string? phone, string? desktop)
        {
            return new Alerte
            {
                AlertGroupId = alertGroupId,
                TitreAlerte = title,
                DescriptionAlerte = description,
                AlertTypeId = alertTypeId,
                PlateformeEnvoieId = plateformeId,
                DestinataireEmail = email,
                DestinatairePhoneNumber = phone,
                DestinataireDesktop = desktop,
                StatutId = 1, // En Cours
                EtatAlerteId = 1, // Non Lu
                DateCreationAlerte = DateTime.UtcNow,
                ProcessedByWorker = false
            };
        }
    }
}
