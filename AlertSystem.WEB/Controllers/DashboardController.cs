using System.Security.Claims;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;
using AlertSystem.WEB.Services;

namespace AlertSystem.WEB.Controllers
{
    [Authorize]
    [Route("Dashboard")]
    public sealed class DashboardController : Controller
    {
        private readonly AlertReadService _read;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUserService;

        // DTO for compose user list
        private sealed record UserListItem(decimal id, decimal userId, string? name, string? email, string? phoneNumber);
        
        public DashboardController(AlertReadService read, ApplicationDbContext db, ICurrentUserService currentUserService) 
        { 
            _read = read; 
            _db = db;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Redirect to Inbox as default view
            return RedirectToAction("Inbox");
        }

        [HttpGet("Inbox")]
        public IActionResult Inbox()
        {
            return View();
        }

        [HttpGet("Sent")]
        public IActionResult Sent()
        {
            return View();
        }

        // Lightweight DTOs for list rendering
        private sealed record AlertListItemDto(
            int Id,
            string? Title,
            string? Message,
            int? AlertTypeId,
            int? StatutId,
            int? EtatAlerteId,
            DateTime DateCreation,
            string? SenderName
        );

        [HttpGet("GetInboxAlerts")]
        public async Task<IActionResult> GetInboxAlerts(DateTime? startDate = null, DateTime? endDate = null, int? typeId = null, int? stateId = null)
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                Response.StatusCode = 401; // Unauthorized (better for frontend detection)
                return Json(new { alerts = Array.Empty<AlertListItemDto>(), total = 0, error = "User not authenticated" });
            }

            var q = _db.Alerte
                .AsNoTracking()
                .Where(a => a.DestinataireUserId == currentUserId.Value);

            if (startDate.HasValue) q = q.Where(a => a.DateCreationAlerte >= startDate.Value);
            if (endDate.HasValue) q = q.Where(a => a.DateCreationAlerte < endDate.Value);
            if (typeId.HasValue) q = q.Where(a => a.AlertTypeId == typeId.Value);
            if (stateId.HasValue) q = q.Where(a => a.EtatAlerteId == stateId.Value);

            // Group by AlertGroupId to avoid duplicates per recipient/platform
            var items = await q
                .GroupBy(a => a.AlertGroupId)
                .OrderByDescending(g => g.Max(a => a.DateCreationAlerte))
                .Take(200)
                .Select(g => new AlertListItemDto(
                    g.Min(a => a.AlertRecordId),
                    g.Select(a => a.TitreAlerte).FirstOrDefault(),
                    g.Select(a => a.DescriptionAlerte).FirstOrDefault(),
                    g.Select(a => (int?)a.AlertTypeId).FirstOrDefault(),
                    g.Max(a => a.StatutId),
                    g.Any(a => a.EtatAlerteId == 1) ? 1 : 2,
                    g.Max(a => a.DateCreationAlerte),
                    _db.DefUtilisateurs
                        .Where(u => u.util_id == g.Select(a => a.ExpediteurId).FirstOrDefault())
                        .Select(u => (u.util_prenom + " " + u.util_nom).Trim())
                        .FirstOrDefault()
                ))
                .ToListAsync();

            return Json(new { alerts = items, total = items.Count });
        }

        [HttpGet("GetOutboxAlerts")]
        public async Task<IActionResult> GetOutboxAlerts(DateTime? startDate = null, DateTime? endDate = null, int? typeId = null, int? stateId = null)
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                Response.StatusCode = 401; // Unauthorized (better for frontend detection)
                return Json(new { alerts = Array.Empty<AlertListItemDto>(), total = 0, error = "User not authenticated" });
            }

            var q = _db.Alerte
                .AsNoTracking()
                .Where(a => a.ExpediteurId == currentUserId.Value);

            if (startDate.HasValue) q = q.Where(a => a.DateCreationAlerte >= startDate.Value);
            if (endDate.HasValue) q = q.Where(a => a.DateCreationAlerte < endDate.Value);
            if (typeId.HasValue) q = q.Where(a => a.AlertTypeId == typeId.Value);
            if (stateId.HasValue) q = q.Where(a => a.StatutId == stateId.Value || a.EtatAlerteId == stateId.Value);

            // 1) First, group by AlertGroupId to collapse platform rows within a send
            var initial = await q
                .GroupBy(a => a.AlertGroupId)
                .OrderByDescending(g => g.Max(a => a.DateCreationAlerte))
                .Take(400)
                .Select(g => new AlertListItemDto(
                    g.Min(a => a.AlertRecordId),
                    g.Select(a => a.TitreAlerte).FirstOrDefault(),
                    g.Select(a => a.DescriptionAlerte).FirstOrDefault(),
                    g.Select(a => (int?)a.AlertTypeId).FirstOrDefault(),
                    g.Max(a => a.StatutId),
                    g.Any(a => a.EtatAlerteId == 1) ? 1 : 2,
                    g.Max(a => a.DateCreationAlerte),
                    _db.DefUtilisateurs
                        .Where(u => u.util_id == g.Select(a => a.ExpediteurId).FirstOrDefault())
                        .Select(u => (u.util_prenom + " " + u.util_nom).Trim())
                        .FirstOrDefault()
                ))
                .ToListAsync();

            // 2) Then, collapse accidental duplicate sends created within the same minute
            var final = initial
                .GroupBy(x => new
                {
                    Title = x.Title ?? string.Empty,
                    Message = x.Message ?? string.Empty,
                    Minute = new DateTime(x.DateCreation.Year, x.DateCreation.Month, x.DateCreation.Day, x.DateCreation.Hour, x.DateCreation.Minute, 0)
                })
                .Select(g => g
                    .OrderByDescending(x => x.DateCreation)
                    .First())
                .OrderByDescending(x => x.DateCreation)
                .Take(200)
                .ToList();

            return Json(new { alerts = final, total = final.Count });
        }

        [HttpGet("InboxKpiData")]
        public async Task<IActionResult> GetInboxKpiData()
        {
            try
            {
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Json(new { receivedToday = 0, unreadAlerts = 0, pendingConfirmation = 0, error = "User not authenticated" });
                }

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Reçues aujourd'hui: nombre d'alertes reçues par l'utilisateur aujourd'hui
                var receivedToday = await _db.Alerte
                    .Where(a => a.DestinataireUserId == currentUserId.Value && 
                               a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow)
                    .CountAsync();

                // Non lues (EtatAlerteId = 1) reçues par l'utilisateur
                var unreadAlerts = await _db.Alerte
                    .Where(a => a.DestinataireUserId == currentUserId.Value && a.EtatAlerteId == 1)
                    .CountAsync();

                // En attente de confirmation (acquittementNecessaire non confirmées) reçues par l'utilisateur
                var pendingConfirmation = await _db.Alerte
                    .Where(a => a.DestinataireUserId == currentUserId.Value && a.EtatAlerteId == 1 && a.AlertTypeId == 2)
                    .CountAsync();

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
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Json(new { sentToday = 0, confirmedAlerts = 0, pendingConfirmation = 0, error = "User not authenticated" });
                }

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Envoyées aujourd'hui: nombre d'alertes envoyées par l'utilisateur aujourd'hui
                var sentToday = await _db.Alerte
                    .Where(a => a.ExpediteurId == currentUserId.Value && 
                               a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow)
                    .Select(a => a.AlertGroupId)
                    .Distinct()
                    .CountAsync();

                // Confirmées: alertes envoyées par l'utilisateur et lues par au moins un destinataire
                var confirmedAlerts = await _db.Alerte
                    .Where(a => a.ExpediteurId == currentUserId.Value && a.EtatAlerteId == 2)
                    .CountAsync();

                // En attente de confirmation: acquittementNecessaire envoyées par l'utilisateur avec au moins un destinataire non lu
                var pendingConfirmation = await _db.Alerte
                    .Where(a => a.ExpediteurId == currentUserId.Value && a.AlertTypeId == 2 && a.EtatAlerteId == 1)
                    .CountAsync();

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
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Json(new { inboxUnreadCount = 0, outboxPendingCount = 0, error = "User not authenticated" });
                }

                // Nombre d'alertes non lues reçues par l'utilisateur
                var inboxUnreadCount = await _db.Alerte
                    .Where(a => a.DestinataireUserId == currentUserId.Value && a.EtatAlerteId == 1)
                    .CountAsync();

                // Nombre d'alertes en attente envoyées par l'utilisateur
                var outboxPendingCount = await _db.Alerte
                    .Where(a => a.ExpediteurId == currentUserId.Value && a.AlertTypeId == 2 && a.EtatAlerteId == 1)
                    .CountAsync();

                return Json(new { inboxUnreadCount, outboxPendingCount });
            }
            catch (Exception ex)
            {
                return Json(new { inboxUnreadCount = 0, outboxPendingCount = 0, error = ex.Message });
            }
        }

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers2()
        {
            // 1) Load application users from def_utilisateur (include grh_emp_id for enrichment)
            var defUsersRaw = await _db.DefUtilisateurs
                .AsNoTracking()
                .Select(u => new
                {
                    id = u.util_id,
                    userId = u.util_id,
                    name = (u.util_prenom + " " + u.util_nom).Trim(),
                    email = u.util_email,
                    empId = u.grh_emp_id,
                    phoneNumber = (string?)null
                })
                .ToListAsync();

            // 2) Load GRH phones (safe raw SQL in case entity isn't mapped)
            var grhPhones = new Dictionary<decimal, string>();
            try
            {
                var conn = _db.Database.GetDbConnection();
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT CAST(e.grh_emp_id AS decimal(18,2)) AS id,
                               NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_gsm, ''))), '') AS phone
                        FROM grh_employe e
                        WHERE NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_gsm, ''))), '') IS NOT NULL
                          AND LTRIM(RTRIM(ISNULL(e.grh_emp_gsm, ''))) NOT IN ('0', '00000000')";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (!reader.IsDBNull(1))
                            {
                                var id = reader.GetDecimal(0);
                                var phone = reader.GetString(1);
                                if (!grhPhones.ContainsKey(id)) grhPhones[id] = phone;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback to alternative table name: grh_employee
                try
                {
                    var conn2 = _db.Database.GetDbConnection();
                    if (conn2.State != System.Data.ConnectionState.Open) await conn2.OpenAsync();
                    using (var cmd2 = conn2.CreateCommand())
                    {
                        cmd2.CommandText = @"
                            SELECT CAST(e.grh_emp_id AS decimal(18,2)) AS id,
                                   NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_gsm, ''))), '') AS phone
                            FROM grh_employee e
                            WHERE NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_gsm, ''))), '') IS NOT NULL
                              AND LTRIM(RTRIM(ISNULL(e.grh_emp_gsm, ''))) NOT IN ('0', '00000000')";
                        using (var reader2 = await cmd2.ExecuteReaderAsync())
                        {
                            while (await reader2.ReadAsync())
                            {
                                if (!reader2.IsDBNull(1))
                                {
                                    var id = reader2.GetDecimal(0);
                                    var phone = reader2.GetString(1);
                                    if (!grhPhones.ContainsKey(id)) grhPhones[id] = phone;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // 3) Enrich def users with GRH phone when possible
            var defUsers = defUsersRaw.Select(u => new UserListItem(
                id: (decimal)u.id,
                userId: (decimal)u.userId,
                name: u.name,
                email: u.email,
                phoneNumber: u.phoneNumber ?? (u.empId.HasValue && grhPhones.TryGetValue((decimal)u.empId.Value, out var p) ? p : null)
            ));
            // Dedup def by email -> phone -> name
            string KeyDef(UserListItem u)
            {
                var email = (u.email ?? string.Empty).Trim().ToLowerInvariant();
                var phone = (u.phoneNumber ?? string.Empty).Trim();
                var name = (u.name ?? string.Empty).Trim().ToLowerInvariant();
                return !string.IsNullOrEmpty(email) ? $"e:{email}" : (!string.IsNullOrEmpty(phone) ? $"p:{phone}" : $"n:{name}");
            }
            var defList = defUsers
                .GroupBy(u => KeyDef(u))
                .Select(g => g.First())
                .OrderBy(u => u.name)
                .ToList();

            // 4) Build standalone GRH employees (complete list), regardless of def link; dedup by phone then name
            var grhList = new List<UserListItem>();
            async Task LoadGrhAsync(string table)
            {
                var conn = _db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    SELECT TOP 5000 
                           CAST(e.grh_emp_id AS decimal(18,2)) AS id,
                           LTRIM(RTRIM(ISNULL(e.grh_emp_prenom, ''))) + ' ' + LTRIM(RTRIM(ISNULL(e.grh_emp_nom,''))) AS name,
                           NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_email, ''))), '') AS email,
                           NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_gsm, ''))), '') AS phoneNumber
                    FROM {table} e
                    WHERE LTRIM(RTRIM(ISNULL(e.grh_emp_prenom, ''))) + ' ' + LTRIM(RTRIM(ISNULL(e.grh_emp_nom,''))) <> ''";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var id = reader.GetDecimal(0);
                    var name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var email = reader.IsDBNull(2) ? null : reader.GetString(2);
                    var phone = reader.IsDBNull(3) ? null : reader.GetString(3);
                    if (!string.IsNullOrWhiteSpace(phone) && phone.Trim() != "00000000" && phone.Trim() != "0")
                    {
                        grhList.Add(new UserListItem(id, id, name, email, phone));
                    }
                    else if (!string.IsNullOrWhiteSpace(name))
                    {
                        grhList.Add(new UserListItem(id, id, name, email, null));
                    }
                }
            }
            // Try multiple table name variants to maximize compatibility
            var tried = new List<string>();
            foreach (var tbl in new[] { "grh_employe", "dbo.grh_employe", "grh_employee", "dbo.grh_employee" })
            {
                try
                {
                    tried.Add(tbl);
                    await LoadGrhAsync(tbl);
                    if (grhList.Count > 0) break;
                }
                catch { /* continue to next variant */ }
            }

            string KeyGrh(UserListItem u)
            {
                var phone = (u.phoneNumber ?? string.Empty).Trim();
                var name = (u.name ?? string.Empty).Trim().ToLowerInvariant();
                return !string.IsNullOrEmpty(phone) ? $"p:{phone}" : $"n:{name}";
            }
            var grhUsers = grhList
                .GroupBy(u => KeyGrh(u))
                .Select(g => g.First())
                .OrderBy(u => u.name)
                .ToList();

            return Json(new { success = true, defUsers = defList, grhUsers });
        }

        

        // Removed legacy duplicate endpoints to avoid route conflicts

        [HttpPost("ConfirmAlert/{alertId}")]
        public async Task<IActionResult> ConfirmAlert(int alertId)
        {
            try
            {
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var alert = await _db.Alerte
                    .FirstOrDefaultAsync(a => a.AlertRecordId == alertId && a.DestinataireUserId == currentUserId.Value);

                if (alert == null)
                {
                    return Json(new { success = false, message = "Alert not found or not accessible" });
                }

                // Mark as read
                alert.EtatAlerteId = 2; // Lu
                alert.DateLecture = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Alert confirmed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("AlertRecipients/{alerteId}")]
        public async Task<IActionResult> GetAlertRecipients(int alerteId)
        {
            try
            {
                // Load the group id for this alert, then group recipients by user and collapse platforms
                var group = await _db.Alerte.Where(x => x.AlertRecordId == alerteId)
                    .Select(x => x.AlertGroupId).FirstOrDefaultAsync();
                if (group == Guid.Empty)
                {
                    return Json(new { recipients = new object[0] });
                }

                var recipients = await _db.Alerte
                    .Include(a => a.DestinataireUser)
                    .Where(a => a.AlertGroupId == group)
                    .GroupBy(a => new { a.DestinataireUserId, Name = a.DestinataireUser != null ? (a.DestinataireUser.util_prenom + " " + a.DestinataireUser.util_nom).Trim() : (string?)null })
                    .Select(g => new
                    {
                        recipientUserId = g.Key.DestinataireUserId,
                        recipientName = g.Key.Name,
                        recipientEmail = g.Where(x => x.DestinataireEmail != null && x.DestinataireEmail != "").Select(x => x.DestinataireEmail).FirstOrDefault(),
                        recipientPhone = g.Where(x => x.DestinatairePhoneNumber != null && x.DestinatairePhoneNumber != "").Select(x => x.DestinatairePhoneNumber).FirstOrDefault(),
                        isRead = g.Any(x => x.EtatAlerteId == 2),
                        readDate = g.Where(x => x.DateLecture != null).OrderBy(x => x.DateLecture).Select(x => x.DateLecture).FirstOrDefault(),
                        status = g.Any(x => x.EtatAlerteId == 2) ? "Lu" : "Non Lu"
                    })
                    .ToListAsync();

                return Json(new { recipients });
            }
            catch (Exception ex)
            {
                return Json(new { recipients = new object[0], error = ex.Message });
            }
        }
    }
}

