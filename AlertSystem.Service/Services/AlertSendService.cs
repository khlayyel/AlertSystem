using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using AlertSystem.Service.Interfaces;
using AlertSystem.Services;
using AlertSystem.Utils.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Data.Common;

namespace AlertSystem.Service.Services
{
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

        public async Task<bool> SendExistingAlertAsync(int alertRecordId, string[] emails, string[] phones, string[] desktop)
        {
            try
            {
                var alert = await _db.Alerte
                    .Include(a => a.TypeEnvoie)
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

                if (alert.PlateformeEnvoieId == 1) // Email
                {
                    if (!string.IsNullOrWhiteSpace(alert.Destinataire))
                    {
                        string senderDisplay = await BuildSenderDisplayAsync(alert) ?? "AlertSystem";

                        var token = _confirmationTokenService.Generate(new ConfirmPayload
                        {
                            AlerteId = (int)alert.AlertRecordId,
                            Kind = "email",
                            Value = alert.Destinataire
                        });
                        var baseUrl = ResolveBaseUrl();
                        var confirmationUrl = $"{baseUrl.TrimEnd('/')}/confirm?t={token}&id={alert.AlertRecordId}";

                        var confirmLabel = (alert.TypeEnvoieId == 2) ? "✅ Confirmer la réception" : "👁️ Marquer comme lu";
                        var htmlContent = _emailTemplateService.CreateAlertEmailTemplate(
                            alert.TitreAlerte,
                            alert.DescriptionAlerte,
                            senderDisplay,
                            DateTime.Now,
                            confirmationUrl,
                            confirmLabel);
                        success = await _notificationService.SendHtmlEmailAsync(alert.Destinataire, alert.TitreAlerte, htmlContent);
                    }
                }
                else if (alert.PlateformeEnvoieId == 2) // WhatsApp
                {
                    if (!string.IsNullOrWhiteSpace(alert.Destinataire))
                    {
                        string senderDisplay = await BuildSenderDisplayAsync(alert) ?? "AlertSystem";
                        var tokenWa = _confirmationTokenService.Generate(new ConfirmPayload
                        {
                            AlerteId = (int)alert.AlertRecordId,
                            Kind = "wa",
                            Value = alert.Destinataire
                        });
                        var baseUrl = ResolveBaseUrl();
                        var confirmationUrl = $"{baseUrl.TrimEnd('/')}/confirm?t={tokenWa}&id={alert.AlertRecordId}";
                        success = await _whatsAppTemplateService.SendAlertTemplateAsync(
                            alert.Destinataire,
                            alert.TitreAlerte,
                            alert.DescriptionAlerte,
                            senderDisplay,
                            confirmationUrl,
                            alert.TypeEnvoieId == 2);
                    }
                }
                else
                {
                    _logger.LogWarning("Unknown platform {PlatformId} for alert {AlertRecordId}", alert.PlateformeEnvoieId, alertRecordId);
                }

                // Update status
                alert.StatutId = success ? 2 : alert.StatutId;
                if (alert.TypeEnvoieId == 2 && success)
                {
                    alert.RappelSuivant = DateTime.UtcNow.AddHours(24);
                }

                await _db.SaveChangesAsync();
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending alert {AlertRecordId}", alertRecordId);
                var alert = await _db.Alerte.FindAsync(alertRecordId);
                if (alert != null)
                {
                    alert.StatutId = 4; // Échoué
                    await _db.SaveChangesAsync();
                }
                return false;
            }
        }

        public async Task<bool> SendAlertAsync(string title, string description, int typeEnvoieId, string[] emails, string[] phones, string[] desktop)
        {
            try
            {
                var alertGroupId = Guid.NewGuid();
                var alerts = new List<Alerte>();

                foreach (var email in emails)
                {
                    alerts.Add(CreateAlertRecord(alertGroupId, title, description, typeEnvoieId, 1, email));
                }

                foreach (var phone in phones)
                {
                    alerts.Add(CreateAlertRecord(alertGroupId, title, description, typeEnvoieId, 2, phone));
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

        private Alerte CreateAlertRecord(Guid alertGroupId, string title, string description, int typeEnvoieId, int plateformeId, string recipient)
        {
            return new Alerte
            {
                AlertGroupId = alertGroupId,
                TitreAlerte = title,
                DescriptionAlerte = description,
                TypeEnvoieId = typeEnvoieId,
                PlateformeEnvoieId = plateformeId,
                Destinataire = recipient,
                StatutId = 1, // En Cours
                EtatId = 1, // Non Lu
                DateCreationAlerte = DateTime.UtcNow,
                ProcessedByWorker = false,
                AppId = 1
            };
        }

        private string ResolveBaseUrl()
        {
            var req = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = _cfg["App:PublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var reqHost = req?.Host.ToString();
                var reqScheme = req?.Scheme;
                if (!string.IsNullOrWhiteSpace(reqHost))
                {
                    var scheme = (reqHost.Contains("localhost", StringComparison.OrdinalIgnoreCase) || reqHost.Contains("127.0.0.1")) ? "http" : (reqScheme ?? "https");
                    baseUrl = $"{scheme}://{reqHost}";
                }
                else
                {
                    baseUrl = "http://localhost:5002";
                }
            }
            return baseUrl;
        }

        private async Task<string?> BuildSenderDisplayAsync(Alerte alert)
        {
            try
            {
                if (alert.ExpediteurId.HasValue)
                {
                    // Best-effort: resolve from def_Utilisateur by UtilisateurId
                    var sender = await _db.DefUtilisateur
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UtilisateurId == (int)alert.ExpediteurId.Value);
                    if (sender != null)
                    {
                        var fullName = sender.Username?.Trim();
                        var email = sender.Email?.Trim();
                        return string.IsNullOrWhiteSpace(email)
                            ? fullName
                            : string.IsNullOrWhiteSpace(fullName) ? email : $"{fullName} <{email}>";
                    }
                }
            }
            catch { }
            return null;
        }

        private async Task<string?> GetApplicationDisplayAsync(int appId)
        {
            try
            {
                await using DbConnection conn = _db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT TOP 1 Description FROM def_App WHERE AppId = @id";
                var p = cmd.CreateParameter();
                p.ParameterName = "@id";
                p.Value = appId;
                cmd.Parameters.Add(p);
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
