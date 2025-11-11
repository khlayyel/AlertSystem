using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using AlertSystem.WEB.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.WEB.Controllers
{
    [Authorize]
    [Route("Dashboard")]
    public sealed class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUserService;

        public DashboardController(ApplicationDbContext db, ICurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public IActionResult Index() => RedirectToAction("Inbox");

        [HttpGet("Inbox")]
        public IActionResult Inbox() => View();

        [HttpGet("Sent")]
        public IActionResult Sent() => View();

        [HttpGet("MyId")]
        public IActionResult MyId()
        {
            var id = _currentUserService.GetCurrentUserId();
            if (!id.HasValue) return Unauthorized();
            return Json(new { userId = id.Value });
        }

        private sealed record AlertListItemDto(long Id, Guid GroupId, string Title, string? Message, int TypeEnvoieId, int StatutId, int EtatId, DateTime CreatedAt, string Platform, string Destinataire);

        private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

        private static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("00")) digits = digits[2..];
            if (digits.Length == 8) digits = "216" + digits;
            return digits.Length > 0 ? "+" + digits : string.Empty;
        }

        private async Task<(DefUtilisateur user, string email, string phone)> GetCurrentUserContextAsync()
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            if (user == null) throw new InvalidOperationException("Utilisateur non authentifié");
            var normalizedEmail = NormalizeEmail(user.Email);
            var normalizedPhone = NormalizePhone(user.WhatsAppNumber);
            return (user, normalizedEmail, normalizedPhone);
        }

        [HttpGet("GetInboxAlerts")]
        public async Task<IActionResult> GetInboxAlerts(DateTime? startDate = null, DateTime? endDate = null, int? typeId = null, int? stateId = null, string? q = null)
        {
            try
            {
                var (user, email, phone) = await GetCurrentUserContextAsync();
                var hasEmail = !string.IsNullOrEmpty(email);
                var hasPhone = !string.IsNullOrEmpty(phone);

                if (!hasEmail && !hasPhone)
                {
                    return Json(new { alerts = Array.Empty<object>(), total = 0 });
                }

                var alertsQuery = _db.Alerte
                    .AsNoTracking()
                    .Where(a => a.AppId == user.AppId);

                if (hasEmail && hasPhone)
                {
                    alertsQuery = alertsQuery.Where(a =>
                        (a.PlateformeEnvoieId == 1 && a.Destinataire.ToLower() == email) ||
                        (a.PlateformeEnvoieId == 2 && a.Destinataire == phone));
                }
                else if (hasEmail)
                {
                    alertsQuery = alertsQuery.Where(a => a.PlateformeEnvoieId == 1 && a.Destinataire.ToLower() == email);
                }
                else if (hasPhone)
                {
                    alertsQuery = alertsQuery.Where(a => a.PlateformeEnvoieId == 2 && a.Destinataire == phone);
                }

                if (startDate.HasValue) alertsQuery = alertsQuery.Where(a => a.DateCreationAlerte >= startDate.Value);
                if (endDate.HasValue) alertsQuery = alertsQuery.Where(a => a.DateCreationAlerte < endDate.Value);
                if (typeId.HasValue) alertsQuery = alertsQuery.Where(a => a.TypeEnvoieId == typeId.Value);
                if (stateId.HasValue) alertsQuery = alertsQuery.Where(a => a.EtatId == stateId.Value);
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var ql = q.Trim().ToLowerInvariant();
                    alertsQuery = alertsQuery.Where(a =>
                        (a.TitreAlerte != null && a.TitreAlerte.ToLower().Contains(ql)) ||
                        (a.DescriptionAlerte != null && a.DescriptionAlerte.ToLower().Contains(ql)));
                }

                var rows = await alertsQuery
                    .OrderByDescending(a => a.DateCreationAlerte)
                    .Take(200)
                    .Select(a => new
                    {
                        a.AlertRecordId,
                        a.AlertGroupId,
                        a.TitreAlerte,
                        a.DescriptionAlerte,
                        a.TypeEnvoieId,
                        a.StatutId,
                        a.EtatId,
                        a.DateCreationAlerte,
                        a.PlateformeEnvoieId,
                        a.Destinataire
                    })
                    .ToListAsync();

                var grouped = rows
                    .GroupBy(a => a.AlertGroupId)
                    .Select(g => new AlertListItemDto(
                        g.OrderByDescending(x => x.DateCreationAlerte).First().AlertRecordId,
                        g.Key,
                        g.Select(x => x.TitreAlerte).FirstOrDefault() ?? "Alerte",
                        g.Select(x => x.DescriptionAlerte).FirstOrDefault(),
                        g.Select(x => x.TypeEnvoieId).FirstOrDefault(),
                        g.Max(x => x.StatutId),
                        g.Any(x => x.EtatId == 1) ? 1 : 2,
                        g.Max(x => x.DateCreationAlerte),
                        string.Join(", ", g.Select(x => x.PlateformeEnvoieId == 1 ? "Email" : "WhatsApp").Distinct()),
                        string.Join(", ", g.Select(x => x.Destinataire).Distinct()))
                    )
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();

                return Json(new { alerts = grouped, total = grouped.Count });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { alerts = Array.Empty<object>(), total = 0, error = ex.Message });
            }
        }

        [HttpGet("GetOutboxAlerts")]
        public async Task<IActionResult> GetOutboxAlerts(DateTime? startDate = null, DateTime? endDate = null, int? typeId = null, int? stateId = null, string? q = null)
        {
            try
            {
                var userId = _currentUserService.GetCurrentUserId();
                if (!userId.HasValue) throw new InvalidOperationException("Utilisateur non authentifié");

                var alertsQuery = _db.Alerte
                    .AsNoTracking()
                    .Where(a => a.ExpediteurId != null && a.ExpediteurId == userId.Value);

                if (startDate.HasValue) alertsQuery = alertsQuery.Where(a => a.DateCreationAlerte >= startDate.Value);
                if (endDate.HasValue) alertsQuery = alertsQuery.Where(a => a.DateCreationAlerte < endDate.Value);
                if (typeId.HasValue) alertsQuery = alertsQuery.Where(a => a.TypeEnvoieId == typeId.Value);
                if (stateId.HasValue) alertsQuery = alertsQuery.Where(a => a.StatutId == stateId.Value || a.EtatId == stateId.Value);
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var ql = q.Trim().ToLowerInvariant();
                    alertsQuery = alertsQuery.Where(a =>
                        (a.TitreAlerte != null && a.TitreAlerte.ToLower().Contains(ql)) ||
                        (a.DescriptionAlerte != null && a.DescriptionAlerte.ToLower().Contains(ql)) ||
                        a.Destinataire.ToLower().Contains(ql));
                }

                var rows = await alertsQuery
                    .OrderByDescending(a => a.DateCreationAlerte)
                    .Take(400)
                    .Select(a => new
                    {
                        a.AlertRecordId,
                        a.AlertGroupId,
                        a.TitreAlerte,
                        a.DescriptionAlerte,
                        a.TypeEnvoieId,
                        a.StatutId,
                        a.EtatId,
                        a.DateCreationAlerte,
                        a.PlateformeEnvoieId,
                        a.Destinataire
                    })
                    .ToListAsync();

                var grouped = rows
                    .GroupBy(a => a.AlertGroupId)
                    .Select(g => new AlertListItemDto(
                        g.OrderByDescending(x => x.DateCreationAlerte).First().AlertRecordId,
                        g.Key,
                        g.Select(x => x.TitreAlerte).FirstOrDefault() ?? "Alerte",
                        g.Select(x => x.DescriptionAlerte).FirstOrDefault(),
                        g.Select(x => x.TypeEnvoieId).FirstOrDefault(),
                        g.Max(x => x.StatutId),
                        g.Any(x => x.EtatId == 1) ? 1 : 2,
                        g.Max(x => x.DateCreationAlerte),
                        string.Join(", ", g.Select(x => x.PlateformeEnvoieId == 1 ? "Email" : "WhatsApp").Distinct()),
                        string.Join(", ", g.Select(x => x.Destinataire).Distinct()))
                    )
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(200)
                    .ToList();

                return Json(new { alerts = grouped, total = grouped.Count });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { alerts = Array.Empty<object>(), total = 0, error = ex.Message });
            }
        }

        [HttpGet("InboxKpiData")]
        public async Task<IActionResult> GetInboxKpiData()
        {
            try
            {
                var (user, email, phone) = await GetCurrentUserContextAsync();
                var hasEmail = !string.IsNullOrEmpty(email);
                var hasPhone = !string.IsNullOrEmpty(phone);

                if (!hasEmail && !hasPhone)
                {
                    return Json(new { receivedToday = 0, unreadAlerts = 0, pendingConfirmation = 0 });
                }

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var baseQuery = _db.Alerte.AsNoTracking().Where(a => a.AppId == user.AppId);
                if (hasEmail && hasPhone)
                {
                    baseQuery = baseQuery.Where(a =>
                        (a.PlateformeEnvoieId == 1 && a.Destinataire.ToLower() == email) ||
                        (a.PlateformeEnvoieId == 2 && a.Destinataire == phone));
                }
                else if (hasEmail)
                {
                    baseQuery = baseQuery.Where(a => a.PlateformeEnvoieId == 1 && a.Destinataire.ToLower() == email);
                }
                else if (hasPhone)
                {
                    baseQuery = baseQuery.Where(a => a.PlateformeEnvoieId == 2 && a.Destinataire == phone);
                }

                var receivedToday = await baseQuery.Where(a => a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow).CountAsync();
                var unreadAlerts = await baseQuery.Where(a => a.EtatId == 1).CountAsync();
                var pendingConfirmation = await baseQuery.Where(a => a.EtatId == 1 && a.TypeEnvoieId == 2).CountAsync();

                return Json(new { receivedToday, unreadAlerts, pendingConfirmation });
            }
            catch (Exception ex)
            {
                return Json(new { receivedToday = 0, unreadAlerts = 0, pendingConfirmation = 0, error = ex.Message });
            }
        }

        [HttpGet("OutboxKpiData")]
        public async Task<IActionResult> GetOutboxKpiData()
        {
            try
            {
                var userId = _currentUserService.GetCurrentUserId();
                if (!userId.HasValue) throw new InvalidOperationException("Utilisateur non authentifié");

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var baseQuery = _db.Alerte.AsNoTracking().Where(a => a.ExpediteurId != null && a.ExpediteurId == userId.Value);

                var sentToday = await baseQuery.Where(a => a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow).Select(a => a.AlertGroupId).Distinct().CountAsync();
                var confirmedAlerts = await baseQuery.GroupBy(a => a.AlertGroupId).CountAsync(g => g.Any(x => x.EtatId == 2));
                var pendingConfirmation = await baseQuery.Where(a => a.TypeEnvoieId == 2 && a.EtatId == 1).GroupBy(a => a.AlertGroupId).CountAsync();

                return Json(new { sentToday, confirmedAlerts, pendingConfirmation });
            }
            catch (Exception ex)
            {
                return Json(new { sentToday = 0, confirmedAlerts = 0, pendingConfirmation = 0, error = ex.Message });
            }
        }

        [HttpGet("SidebarCounts")]
        public async Task<IActionResult> GetSidebarCounts()
        {
            try
            {
                var (user, email, phone) = await GetCurrentUserContextAsync();
                var hasEmail = !string.IsNullOrEmpty(email);
                var hasPhone = !string.IsNullOrEmpty(phone);

                if (!hasEmail && !hasPhone)
                {
                    return Json(new { inboxUnreadCount = 0, outboxPendingCount = 0 });
                }

                var baseQuery = _db.Alerte.AsNoTracking().Where(a => a.AppId == user.AppId);
                if (hasEmail && hasPhone)
                {
                    baseQuery = baseQuery.Where(a =>
                        (a.PlateformeEnvoieId == 1 && a.Destinataire.ToLower() == email) ||
                        (a.PlateformeEnvoieId == 2 && a.Destinataire == phone));
                }
                else if (hasEmail)
                {
                    baseQuery = baseQuery.Where(a => a.PlateformeEnvoieId == 1 && a.Destinataire.ToLower() == email);
                }
                else if (hasPhone)
                {
                    baseQuery = baseQuery.Where(a => a.PlateformeEnvoieId == 2 && a.Destinataire == phone);
                }

                var inboxUnreadCount = await baseQuery.Where(a => a.EtatId == 1).CountAsync();
                var outboxPendingCount = await _db.Alerte.AsNoTracking()
                    .Where(a => a.ExpediteurId != null && a.ExpediteurId == user.UtilisateurId && a.TypeEnvoieId == 2 && a.EtatId == 1)
                    .CountAsync();

                return Json(new { inboxUnreadCount, outboxPendingCount });
            }
            catch (Exception ex)
            {
                return Json(new { inboxUnreadCount = 0, outboxPendingCount = 0, error = ex.Message });
            }
        }

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var defUsers = await _db.DefUtilisateur
                .AsNoTracking()
                .Select(u => new
                {
                    id = u.UtilisateurId,
                    userId = u.UtilisateurId,
                    name = u.Username,
                    email = u.Email,
                    phoneNumber = u.WhatsAppNumber,
                    appId = u.AppId
                })
                .OrderBy(u => u.name)
                .ToListAsync();

            return Json(new { success = true, defUsers, grhUsers = Array.Empty<object>() });
        }

        [HttpPost("ConfirmAlert/{alertId}")]
        public async Task<IActionResult> ConfirmAlert(long alertId)
        {
            try
            {
                var (user, email, phone) = await GetCurrentUserContextAsync();

                var alert = await _db.Alerte.FirstOrDefaultAsync(a => a.AlertRecordId == alertId);
                if (alert == null)
                {
                    return Json(new { success = false, message = "Alerte introuvable" });
                }

                var isRecipient = (alert.PlateformeEnvoieId == 1 && alert.Destinataire.ToLower() == email) ||
                                  (alert.PlateformeEnvoieId == 2 && alert.Destinataire == phone);

                if (!isRecipient)
                {
                    return Json(new { success = false, message = "Alerte non accessible" });
                }

                var rows = await _db.Alerte
                    .Where(a => a.AlertGroupId == alert.AlertGroupId &&
                                ((a.PlateformeEnvoieId == 1 && a.Destinataire.ToLower() == email) ||
                                 (a.PlateformeEnvoieId == 2 && a.Destinataire == phone)))
                    .ToListAsync();

                foreach (var row in rows)
                {
                    if (row.EtatId != 2)
                    {
                        row.EtatId = 2;
                        row.DateLecture = DateTime.UtcNow;
                    }
                }
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Alerte confirmée" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("AlertRecipients/{alerteId}")]
        public async Task<IActionResult> GetAlertRecipients(long alerteId)
        {
            try
            {
                var groupId = await _db.Alerte
                    .Where(a => a.AlertRecordId == alerteId)
                    .Select(a => a.AlertGroupId)
                    .FirstOrDefaultAsync();

                if (groupId == Guid.Empty)
                {
                    return Json(new { recipients = Array.Empty<object>() });
                }

                var recipients = await _db.Alerte
                    .AsNoTracking()
                    .Where(a => a.AlertGroupId == groupId)
                    .Select(a => new
                    {
                        a.Destinataire,
                        Platform = a.PlateformeEnvoieId == 1 ? "Email" : "WhatsApp",
                        IsRead = a.EtatId != 1,
                        a.DateLecture
                    })
                    .ToListAsync();

                return Json(new { recipients });
            }
            catch (Exception ex)
            {
                return Json(new { recipients = Array.Empty<object>(), error = ex.Message });
            }
        }
    }
}

