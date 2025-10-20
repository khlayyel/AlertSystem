using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlertSystem.Data;
using AlertSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Service
{
    public interface IAlertSendService
    {
        Task<(int alerteId, bool anySuccess)> SendManualAsync(
            string title,
            string message,
            IEnumerable<string> emails,
            IEnumerable<string> phones,
            bool sendEmail,
            bool sendWhatsApp,
            bool sendDesktop,
            IEnumerable<int>? userIds = null,
            int? alertTypeId = null);
    }

    public sealed class AlertSendService : IAlertSendService
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notify;
        private readonly AlertSystem.Services.IWhatsAppService _wa;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _cfg;
        private readonly ConfirmationTokenService _tokens;
        private readonly IEmailTemplateService _emailTemplate;

        public AlertSendService(ApplicationDbContext db, INotificationService notify, AlertSystem.Services.IWhatsAppService wa, Microsoft.Extensions.Configuration.IConfiguration cfg, ConfirmationTokenService tokens, IEmailTemplateService emailTemplate)
        {
            _db = db;
            _notify = notify;
            _wa = wa;
            _cfg = cfg;
            _tokens = tokens;
            _emailTemplate = emailTemplate;
        }

        public async Task<(int alerteId, bool anySuccess)> SendManualAsync(
            string title,
            string message,
            IEnumerable<string> emails,
            IEnumerable<string> phones,
            bool sendEmail,
            bool sendWhatsApp,
            bool sendDesktop,
            IEnumerable<int>? userIds = null,
            int? alertTypeId = null)
        {
            Console.WriteLine($"SendManualAsync called: sendEmail={sendEmail}, emails={emails?.Count() ?? 0}, phones={phones?.Count() ?? 0}");
            var now = DateTime.UtcNow;
            // Resolve default ExpedTypeId to Service to satisfy FK
            var expedTypeId = await _db.ExpedType
                .AsNoTracking()
                .Where(x => x.ExpedTypeName == "Humain")
                .Select(x => x.ExpedTypeId)
                .FirstOrDefaultAsync();
            if (expedTypeId == 0)
            {
                expedTypeId = await _db.ExpedType
                    .AsNoTracking()
                    .Select(x => x.ExpedTypeId)
                    .OrderBy(id => id)
                    .FirstOrDefaultAsync();
                if (expedTypeId == 0) expedTypeId = 1; // last resort
            }

            var alerte = new AlertSystem.Entities.Entities.Alerte
            {
                TitreAlerte = title,
                DescriptionAlerte = message,
                DateCreationAlerte = now,
                StatutId = 1, // En Cours
                AlertTypeId = alertTypeId ?? 1,
                AppId = null,
                PlateformeEnvoieId = null,
                ExpedTypeId = expedTypeId,
                EtatAlerteId = 1 // Non Lu (required by FK)
            };

            _db.Alerte.Add(alerte);
            await _db.SaveChangesAsync();

            var success = false;

            if (sendEmail)
            {
                Console.WriteLine($"Starting email sending for {emails?.Count() ?? 0} emails");
                foreach (var e in (emails ?? Array.Empty<string>()))
                {
                    var email = (e ?? string.Empty).Trim();
                    Console.WriteLine($"Processing email: '{email}'");
                    if (string.IsNullOrWhiteSpace(email)) 
                    {
                        Console.WriteLine("Skipping empty email");
                        continue;
                    }
                    // Generate confirmation token for this email recipient
                    var baseUrl = _cfg["BASE_URL"]?.TrimEnd('/') ?? "http://localhost:5185";
                    var confirmToken = _tokens.Generate(new ConfirmPayload 
                    { 
                        AlerteId = alerte.AlerteId, 
                        Kind = "email", 
                        Value = email 
                    });
                    var confirmUrl = $"{baseUrl}/confirm?t={confirmToken}";

                    _db.HistoriqueAlertes.Add(new AlertSystem.Entities.Entities.HistoriqueAlerte
                    {
                        AlerteId = alerte.AlerteId,
                        DestinataireEmail = email,
                        EtatAlerteId = 1
                    });
                    
                    try 
                    { 
                        // Create professional email template
                        var senderName = "Système d'Alerte";
                        var emailHtml = _emailTemplate.CreateAlertEmailTemplate(title, message, senderName, alerte.DateCreationAlerte, confirmUrl);
                        success = success || await _notify.SendHtmlEmailAsync(email, $"🚨 {title}", emailHtml); 
                    }
                    catch (Exception ex) { 
                        // Log the email sending error but continue with other emails
                        Console.WriteLine($"Email sending failed for {email}: {ex.Message}");
                    }
                }
            }

            if (sendWhatsApp)
            {
                foreach (var p in (phones ?? Array.Empty<string>()))
                {
                    var phone = (p ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(phone)) continue;
                    _db.HistoriqueAlertes.Add(new AlertSystem.Entities.Entities.HistoriqueAlerte
                    {
                        AlerteId = alerte.AlerteId,
                        DestinatairePhoneNumber = phone,
                        EtatAlerteId = 1
                    });
                    try {
                        // Build confirmation tokenized URL for WA template button (if template supports URL variable)
                        var baseUrl = _cfg["BASE_URL"]?.TrimEnd('/') ?? "http://localhost:5185";
                        var tok = _tokens.Generate(new ConfirmPayload { AlerteId = alerte.AlerteId, Kind = "wa", Value = phone });
                        var confirmUrl = $"{baseUrl}/confirm?t={tok}";
                        var templateName = _cfg["WhatsApp:DefaultTemplateName"];
                        var templateLang = _cfg["WhatsApp:DefaultTemplateLang"] ?? "en_US";

                        bool delivered;
                        if (!string.IsNullOrWhiteSpace(templateName))
                        {
                            // Force template-first strategy to reach new contacts reliably
                            var vars = new Dictionary<string, string>
                            {
                                { "alert_title", title },
                                { "alert_body", message },
                                { "confirm_url", confirmUrl }
                            };
                            delivered = await _wa.SendTemplateAsync(phone, templateName!, templateLang, vars);
                            if (!delivered)
                            {
                                // Fallback to hello_world if dynamic template not approved/available
                                delivered = await _wa.SendTemplateAsync(phone, "hello_world", templateLang, null);
                                if (!delivered)
                                {
                                    // Last try: free-form within 24h window
                                    delivered = await _notify.SendWhatsAppAsync(phone, $"{title}\n\n{message}");
                                }
                            }
                        }
                        else
                        {
                            // Legacy: free-form then hello_world
                            delivered = await _notify.SendWhatsAppAsync(phone, $"{title}\n\n{message}");
                            if (!delivered)
                            {
                                delivered = await _wa.SendTemplateAsync(phone, "hello_world", templateLang, null);
                            }
                        }
                        success = success || delivered;
                    }
                    catch (Exception ex) { 
                        // Log the WhatsApp sending error but continue with other phones
                        Console.WriteLine($"WhatsApp sending failed for {phone}: {ex.Message}");
                    }
                }
            }

            if (sendDesktop && userIds != null)
            {
                // Only create Historique for valid existing users (avoid FK conflicts)
                var requestedIds = userIds.Where(id => id > 0).Distinct().ToArray();
                var existingIds = await _db.Users
                    .Where(u => requestedIds.Contains(u.UserId))
                    .Select(u => u.UserId)
                    .ToListAsync();

                foreach (var uid in existingIds)
                {
                    _db.HistoriqueAlertes.Add(new AlertSystem.Entities.Entities.HistoriqueAlerte
                    {
                        AlerteId = alerte.AlerteId,
                        DestinataireUserId = uid,
                        EtatAlerteId = 1
                    });
                    try { success = success || await _notify.SendPushNotificationAsync(uid, title, message); }
                    catch { }
                }
                // If no existing user IDs, do not insert desktop history to avoid FK violation
            }

            await _db.SaveChangesAsync();

            alerte.StatutId = success ? 2 : 4; // Envoyé : Échoué
            await _db.SaveChangesAsync();

            return (alerte.AlerteId, success);
        }
    }
}


