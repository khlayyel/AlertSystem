using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Worker.Services
{
    public sealed class UserRecipientService : IUserRecipientService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UserRecipientService> _logger;

        public UserRecipientService(ApplicationDbContext db, ILogger<UserRecipientService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<(int PlatformId, string Recipient)>> GetRecipientsForUserAsync(int utilisateurId, CancellationToken ct)
        {
            var user = await _db.DefUtilisateur
                .Where(u => u.UtilisateurId == utilisateurId)
                .Select(u => new { u.Email, u.WhatsAppNumber })
                .FirstOrDefaultAsync(ct);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", utilisateurId);
                return new List<(int, string)>();
            }

            var recipients = new List<(int PlatformId, string Recipient)>();

            // Email (platform 1)
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var email = user.Email.Trim().ToLowerInvariant();
                recipients.Add((1, email));
            }

            // WhatsApp (platform 2)
            if (!string.IsNullOrWhiteSpace(user.WhatsAppNumber))
            {
                var digits = new string(user.WhatsAppNumber.Where(char.IsDigit).ToArray());
                if (digits.StartsWith("00"))
                {
                    digits = digits.Substring(2);
                }
                if (digits.Length == 8)
                {
                    digits = "216" + digits;
                }
                if (!digits.StartsWith("216") && digits.Length > 0)
                {
                    // fallback: keep as-is
                }
                if (digits.Length > 0)
                {
                    var formatted = "+" + digits;
                    recipients.Add((2, formatted));
                }
            }

            _logger.LogInformation("Found {Count} recipients for user {UserId}: {Recipients}",
                recipients.Count, utilisateurId, string.Join(", ", recipients.Select(r => $"{r.PlatformId}:{r.Recipient}")));

            return recipients;
        }
    }
}

