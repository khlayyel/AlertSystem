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

        public AlertSendService(ApplicationDbContext db, INotificationService notify)
        {
            _db = db;
            _notify = notify;
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
            var now = DateTime.UtcNow;
            var alerte = new AlertSystem.Entities.Entities.Alerte
            {
                TitreAlerte = title,
                DescriptionAlerte = message,
                DateCreationAlerte = now,
                StatutId = 1, // En Cours
                AlertTypeId = alertTypeId ?? 1,
                AppId = null,
                PlateformeEnvoieId = null
            };

            _db.Alerte.Add(alerte);
            await _db.SaveChangesAsync();

            var success = false;

            if (sendEmail)
            {
                foreach (var e in (emails ?? Array.Empty<string>()))
                {
                    var email = (e ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(email)) continue;
                    _db.HistoriqueAlertes.Add(new AlertSystem.Entities.Entities.HistoriqueAlerte
                    {
                        AlerteId = alerte.AlerteId,
                        DestinataireEmail = email,
                        EtatAlerte = "Non Lu"
                    });
                    try { success = success || await _notify.SendEmailAsync(email, title, message); }
                    catch { }
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
                        EtatAlerte = "Non Lu"
                    });
                    try { success = success || await _notify.SendWhatsAppAsync(phone, $"{title}\n\n{message}"); }
                    catch { }
                }
            }

            if (sendDesktop && userIds != null)
            {
                foreach (var uid in userIds)
                {
                    _db.HistoriqueAlertes.Add(new AlertSystem.Entities.Entities.HistoriqueAlerte
                    {
                        AlerteId = alerte.AlerteId,
                        DestinataireUserId = uid,
                        EtatAlerte = "Non Lu"
                    });
                    try { success = success || await _notify.SendPushNotificationAsync(uid, title, message); }
                    catch { }
                }
            }

            await _db.SaveChangesAsync();

            alerte.StatutId = success ? 2 : 4; // Envoyé : Échoué
            await _db.SaveChangesAsync();

            return (alerte.AlerteId, success);
        }
    }
}


