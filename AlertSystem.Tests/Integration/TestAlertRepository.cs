using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlertSystem.Worker.Models;
using AlertSystem.Worker.Services;
using AlertSystem.Data;
using AlertSystem.Entities.Entities;

namespace AlertSystem.Tests.Integration
{
    public class TestAlertRepository : IAlertRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AlertRepository> _logger;

        public TestAlertRepository(ApplicationDbContext db, ILogger<AlertRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<AlerteModel>> GetUnprocessedAlertsAsync(CancellationToken cancellationToken = default)
        {
            var alerts = await _db.Alerte
                .Where(a => a.StatutId == 1 || a.StatutId == 4) // En Cours or Échoué
                .Select(a => new AlerteModel
                {
                    AlerteId = a.AlerteId,
                    TitreAlerte = a.TitreAlerte,
                    DescriptionAlerte = a.DescriptionAlerte ?? string.Empty,
                    AlertTypeId = a.AlertTypeId,
                    DateCreationAlerte = a.DateCreationAlerte,
                    StatutId = a.StatutId,
                    DestinataireId = a.DestinataireId,
                    PlateformeEnvoieId = a.PlateformeEnvoieId,
                    ProcessedByWorker = false
                })
                .ToListAsync(cancellationToken);

            return alerts;
        }

        public async Task<bool> UpdateAlertStatusAsync(int alerteId, int newStatusId, CancellationToken cancellationToken = default)
        {
            var alert = await _db.Alerte.FindAsync(alerteId);
            if (alert == null) return false;

            alert.StatutId = newStatusId;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task SetInitialReminderAsync(int alerteId, int intervalMinutes, CancellationToken cancellationToken = default)
        {
            var alert = await _db.Alerte.FindAsync(alerteId);
            if (alert == null) return;

            // Create initial reminder entry
            var reminder = new RappelSuivant
            {
                AlerteId = alerteId,
                DateRappel = DateTime.UtcNow.AddMinutes(intervalMinutes),
                StatutRappel = "En Attente",
                Tentative = 1,
                DetailsErreur = null
            };

            _db.RappelSuivant.Add(reminder);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> UpdateReminderStatusAsync(int alerteId, bool success, int intervalMinutes, int maxAttempts, CancellationToken cancellationToken = default)
        {
            var alert = await _db.Alerte.FindAsync(alerteId);
            if (alert == null) return false;

            // Find the latest reminder for this alert
            var latestReminder = await _db.RappelSuivant
                .Where(r => r.AlerteId == alerteId)
                .OrderByDescending(r => r.DateRappel)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestReminder == null) return false;

            // Update the reminder status
            latestReminder.StatutRappel = success ? "Envoyé" : "Échoué";
            latestReminder.DetailsErreur = success ? null : "Test failure";

            if (!success && latestReminder.Tentative < maxAttempts)
            {
                // Schedule next reminder
                var nextReminder = new RappelSuivant
                {
                    AlerteId = alerteId,
                    DateRappel = DateTime.UtcNow.AddMinutes(intervalMinutes),
                    StatutRappel = "En Attente",
                    Tentative = latestReminder.Tentative + 1,
                    DetailsErreur = null
                };
                _db.RappelSuivant.Add(nextReminder);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }


        public async Task<List<UserModel>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await _db.Users
                .Where(u => u.IsActive)
                .Select(u => new UserModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    DesktopDeviceToken = u.DesktopDeviceToken,
                    IsActive = u.IsActive
                })
                .ToListAsync(cancellationToken);

            return users;
        }

        public async Task<UserModel?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .Where(u => u.UserId == userId)
                .Select(u => new UserModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    DesktopDeviceToken = u.DesktopDeviceToken,
                    IsActive = u.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);

            return user;
        }

        public async Task MarkAlertAsProcessedAsync(int alerteId, CancellationToken cancellationToken = default)
        {
            var alert = await _db.Alerte.FindAsync(alerteId);
            if (alert != null)
            {
                alert.StatutId = 2; // Envoyé
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task MarkAlertAsFailedAsync(int alerteId, CancellationToken cancellationToken = default)
        {
            var alert = await _db.Alerte.FindAsync(alerteId);
            if (alert != null)
            {
                alert.StatutId = 4; // Échoué
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task CreateHistoriqueAlerteAsync(int alerteId, int userId, string email, string phoneNumber, string desktopToken, CancellationToken cancellationToken = default)
        {
            var historique = new HistoriqueAlerte
            {
                AlerteId = alerteId,
                DestinataireUserId = userId,
                DestinataireEmail = email,
                DestinatairePhoneNumber = phoneNumber,
                DestinataireDesktop = desktopToken,
                EtatAlerteId = 1, // Non Lu
                DateLecture = null
            };

            _db.HistoriqueAlertes.Add(historique);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<AlerteModel>> GetReminderAlertsAsync(CancellationToken cancellationToken = default)
        {
            var alerts = await _db.Alerte
                .Where(a => a.AlertTypeId == 2) // acquittementNécessaire
                .Select(a => new AlerteModel
                {
                    AlerteId = a.AlerteId,
                    TitreAlerte = a.TitreAlerte,
                    DescriptionAlerte = a.DescriptionAlerte ?? string.Empty,
                    AlertTypeId = a.AlertTypeId,
                    DateCreationAlerte = a.DateCreationAlerte,
                    StatutId = a.StatutId,
                    DestinataireId = a.DestinataireId,
                    PlateformeEnvoieId = a.PlateformeEnvoieId,
                    ProcessedByWorker = false
                })
                .ToListAsync(cancellationToken);

            return alerts;
        }


        public async Task InsertReminderHistoryAsync(int alerteId, int historiqueAlerteId, bool success, int attemptNumber, string? errorDetails, CancellationToken cancellationToken = default)
        {
            var reminder = new RappelSuivant
            {
                AlerteId = alerteId,
                HistoriqueAlerteId = historiqueAlerteId,
                DateRappel = DateTime.UtcNow,
                StatutRappel = success ? "Envoyé" : "Échoué",
                Tentative = attemptNumber,
                DetailsErreur = errorDetails
            };

            _db.RappelSuivant.Add(reminder);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<int>> GetUnconfirmedRecipientsAsync(int alerteId, CancellationToken cancellationToken = default)
        {
            var recipients = await _db.HistoriqueAlertes
                .Where(h => h.AlerteId == alerteId && h.EtatAlerteId == 1) // Non Lu
                .Select(h => h.DestinataireUserId ?? 0)
                .Where(userId => userId > 0)
                .ToListAsync(cancellationToken);

            return recipients;
        }
    }
}
