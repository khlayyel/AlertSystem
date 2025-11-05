using System.Security.Claims;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;
using AlertSystem.WEB.Services;
using Microsoft.AspNetCore.SignalR;

namespace AlertSystem.WEB.Controllers
{
    [Authorize]
    [Route("Dashboard")]
        public sealed class DashboardController : Controller
    {
        private readonly AlertReadService _read;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUserService;
            private readonly IHubContext<AlertSystem.Infrastructure.Hubs.NotificationHub> _hub;

        // DTO for compose user list
        private sealed record UserListItem(decimal id, decimal userId, string? name, string? email, string? phoneNumber);
        
        public DashboardController(AlertReadService read, ApplicationDbContext db, ICurrentUserService currentUserService, IHubContext<AlertSystem.Infrastructure.Hubs.NotificationHub> hub) 
        { 
            _read = read; 
            _db = db;
            _currentUserService = currentUserService;
            _hub = hub;
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

        [HttpGet("MyId")]
        public IActionResult MyId()
        {
            var id = _currentUserService.GetCurrentUserId();
            if (!id.HasValue) return Unauthorized();
            return Json(new { userId = id.Value });
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
        public async Task<IActionResult> GetInboxAlerts(DateTime? startDate = null, DateTime? endDate = null, int? typeId = null, int? stateId = null, string? q = null)
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                Response.StatusCode = 401; // Unauthorized (better for frontend detection)
                return Json(new { alerts = Array.Empty<AlertListItemDto>(), total = 0, error = "User not authenticated" });
            }

            var query = _db.Alerte
                .AsNoTracking()
                .Where(a => a.DestinataireUserId == currentUserId.Value);

            if (startDate.HasValue) query = query.Where(a => a.DateCreationAlerte >= startDate.Value);
            if (endDate.HasValue) query = query.Where(a => a.DateCreationAlerte < endDate.Value);
            if (typeId.HasValue) query = query.Where(a => a.AlertTypeId == typeId.Value);
            if (stateId.HasValue) query = query.Where(a => a.EtatAlerteId == stateId.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var ql = q.Trim().ToLower();
                query = query.Where(a =>
                    (a.TitreAlerte != null && a.TitreAlerte.ToLower().Contains(ql)) ||
                    (a.DescriptionAlerte != null && a.DescriptionAlerte.ToLower().Contains(ql)) ||
                    (((_db.DefUtilisateurs
                        .Where(u => u.util_id == a.ExpediteurId)
                        .Select(u => ((u.util_prenom ?? "") + " " + (u.util_nom ?? "")))
                        .FirstOrDefault() ?? "").ToLower()).Contains(ql))
                );
            }

            // Group by AlertGroupId to avoid duplicates per recipient/platform
            var items = await query
                .GroupBy(a => a.AlertGroupId)
                .OrderByDescending(g => g.Max(a => a.DateCreationAlerte))
                .Take(200)
                .Select(g => new AlertListItemDto(
                    g.Min(a => a.AlertRecordId),
                    g.Select(a => a.TitreAlerte).FirstOrDefault(),
                    g.Select(a => a.DescriptionAlerte).FirstOrDefault(),
                    g.Select(a => (int?)a.AlertTypeId).FirstOrDefault(),
                    g.Max(a => a.StatutId),
                    // FIX: Only count current logged-in user's read state
                    g.Where(a => a.DestinataireUserId == currentUserId.Value).Any(a => a.EtatAlerteId == 1) ? 1 : 2,
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
        public async Task<IActionResult> GetOutboxAlerts(DateTime? startDate = null, DateTime? endDate = null, int? typeId = null, int? stateId = null, string? q = null)
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                Response.StatusCode = 401; // Unauthorized (better for frontend detection)
                return Json(new { alerts = Array.Empty<AlertListItemDto>(), total = 0, error = "User not authenticated" });
            }

            var query = _db.Alerte
                .AsNoTracking()
                .Where(a => a.ExpediteurId == currentUserId.Value);

            if (startDate.HasValue) query = query.Where(a => a.DateCreationAlerte >= startDate.Value);
            if (endDate.HasValue) query = query.Where(a => a.DateCreationAlerte < endDate.Value);
            if (typeId.HasValue) query = query.Where(a => a.AlertTypeId == typeId.Value);
            if (stateId.HasValue) query = query.Where(a => a.StatutId == stateId.Value || a.EtatAlerteId == stateId.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var ql = q.Trim().ToLower();
                query = query.Where(a =>
                    (a.TitreAlerte != null && a.TitreAlerte.ToLower().Contains(ql)) ||
                    (a.DescriptionAlerte != null && a.DescriptionAlerte.ToLower().Contains(ql)) ||
                    (a.DestinataireEmail != null && a.DestinataireEmail.ToLower().Contains(ql)) ||
                    (a.DestinatairePhoneNumber != null && a.DestinatairePhoneNumber.ToLower().Contains(ql)) ||
                    (((_db.DefUtilisateurs
                        .Where(u => u.util_id == a.DestinataireUserId)
                        .Select(u => ((u.util_prenom ?? "") + " " + (u.util_nom ?? "")))
                        .FirstOrDefault() ?? "").ToLower()).Contains(ql))
                );
            }

            // 1) First, group by AlertGroupId to collapse platform rows within a send
            var initial = await query
                .GroupBy(a => a.AlertGroupId)
                .OrderByDescending(g => g.Max(a => a.DateCreationAlerte))
                .Take(400)
                .Select(g => new AlertListItemDto(
                    g.Min(a => a.AlertRecordId),
                    g.Select(a => a.TitreAlerte).FirstOrDefault(),
                    g.Select(a => a.DescriptionAlerte).FirstOrDefault(),
                    g.Select(a => (int?)a.AlertTypeId).FirstOrDefault(),
                    g.Max(a => a.StatutId),
                    // Outbox read/confirm state for the group: show Confirmé/Lu if ANY recipient confirmed
                    g.Any(a => a.EtatAlerteId == 2) ? 2 : 1,
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

                // Confirmées: compter les groupes envoyés par l'utilisateur où AU MOINS un destinataire a confirmé (EtatAlerteId = 2)
                var confirmedAlerts = await _db.Alerte
                    .Where(a => a.ExpediteurId == currentUserId.Value)
                    .GroupBy(a => a.AlertGroupId)
                    .CountAsync(g => g.Any(x => x.EtatAlerteId == 2));

                // En attente de confirmation: nombre d’alertes obligatoires envoyées PAR L’UTILISATEUR pour lesquelles il reste au moins un non-confirmé
                var pendingConfirmation = await _db.Alerte
                    .Where(a => a.ExpediteurId == currentUserId.Value && a.AlertTypeId == 2)
                    .GroupBy(a => a.AlertGroupId)
                    .CountAsync(g => g.Any(x => x.EtatAlerteId == 1));

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

            // Build mapping from GRH employee to system user when available
            var grhEmpIdToUserId = defUsersRaw
                .Where(u => u.empId.HasValue)
                .GroupBy(u => (decimal)u.empId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => (decimal)x.id).First());

            // 2) Load GRH phones (safe raw SQL in case entity isn't mapped)
            var grhPhones = new Dictionary<decimal, string>();
            try
            {
                // Try simplest compatible SQL first (only grh_emp_gsm)
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

                // Best-effort enrichment with alternative columns (ignore if columns don't exist)
                try
                {
                    using var cmd2 = _db.Database.GetDbConnection().CreateCommand();
                    if (cmd2.Connection!.State != System.Data.ConnectionState.Open) await cmd2.Connection.OpenAsync();
                    cmd2.CommandText = @"
                        SELECT CAST(e.grh_emp_id AS decimal(18,2)) AS id,
                               NULLIF(LTRIM(RTRIM(
                                   COALESCE(NULLIF(e.grh_emp_gsm,''), NULLIF(e.grh_emp_tel,''), NULLIF(e.grh_emp_telephone,''), NULLIF(e.gsm,''), NULLIF(e.tel,''), '')
                               )), '') AS phone
                        FROM grh_employe e";
                    using var r2 = await cmd2.ExecuteReaderAsync();
                    while (await r2.ReadAsync())
                    {
                        if (!r2.IsDBNull(1))
                        {
                            var id = r2.GetDecimal(0);
                            var phone = r2.GetString(1);
                            if (!grhPhones.ContainsKey(id)) grhPhones[id] = phone;
                        }
                    }
                }
                catch { /* ignore enrichment errors */ }
            }
            catch
            {
                // Fallback to alternative table name: grh_employee (simple first)
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

                    // Try enrichment with alternative columns on this table
                    try
                    {
                        using var cmd3 = conn2.CreateCommand();
                        cmd3.CommandText = @"
                            SELECT CAST(e.grh_emp_id AS decimal(18,2)) AS id,
                                   NULLIF(LTRIM(RTRIM(
                                       COALESCE(NULLIF(e.grh_emp_gsm,''), NULLIF(e.grh_emp_tel,''), NULLIF(e.grh_emp_telephone,''), NULLIF(e.gsm,''), NULLIF(e.tel,''), '')
                                   )), '') AS phone
                            FROM grh_employee e";
                        using var r3 = await cmd3.ExecuteReaderAsync();
                        while (await r3.ReadAsync())
                        {
                            if (!r3.IsDBNull(1))
                            {
                                var id = r3.GetDecimal(0);
                                var phone = r3.GetString(1);
                                if (!grhPhones.ContainsKey(id)) grhPhones[id] = phone;
                            }
                        }
                    }
                    catch { }
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
                // Try simple select first (only gsm)
                var sqls = new[] {
                    $@"SELECT TOP 5000 CAST(e.grh_emp_id AS decimal(18,2)) AS id,
                        LTRIM(RTRIM(ISNULL(e.grh_emp_prenom, ''))) + ' ' + LTRIM(RTRIM(ISNULL(e.grh_emp_nom,''))) AS name,
                        NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_email, ''))), '') AS email,
                        NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_gsm, ''))), '') AS phoneNumber
                       FROM {table} e
                       WHERE LTRIM(RTRIM(ISNULL(e.grh_emp_prenom, ''))) + ' ' + LTRIM(RTRIM(ISNULL(e.grh_emp_nom,''))) <> ''",
                    $@"SELECT TOP 5000 CAST(e.grh_emp_id AS decimal(18,2)) AS id,
                        LTRIM(RTRIM(ISNULL(e.grh_emp_prenom, ''))) + ' ' + LTRIM(RTRIM(ISNULL(e.grh_emp_nom,''))) AS name,
                        NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_email, ''))), '') AS email,
                        NULLIF(LTRIM(RTRIM(COALESCE(NULLIF(e.grh_emp_gsm,''), NULLIF(e.grh_emp_tel,''), NULLIF(e.grh_emp_telephone,''), NULLIF(e.gsm,''), NULLIF(e.tel,''), ''))), '') AS phoneNumber
                       FROM {table} e
                       WHERE LTRIM(RTRIM(ISNULL(e.grh_emp_prenom, ''))) + ' ' + LTRIM(RTRIM(ISNULL(e.grh_emp_nom,''))) <> ''"
                };
                foreach (var sql in sqls)
                {
                    try
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = sql;
                        using var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            var id = reader.GetDecimal(0);
                            var name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            var email = reader.IsDBNull(2) ? null : reader.GetString(2);
                            var phone = reader.IsDBNull(3) ? null : reader.GetString(3);
                            decimal? mappedUserId = grhEmpIdToUserId.TryGetValue(id, out var uid) ? uid : (decimal?)null;
                            grhList.Add(new UserListItem(id, mappedUserId ?? id, name, email, phone));
                        }
                        if (grhList.Count > 0) break;
                    }
                    catch { /* try next variant */ }
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

                // Resolve alert and group
                var target = await _db.Alerte
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AlertRecordId == alertId && a.DestinataireUserId == currentUserId.Value);

                if (target == null)
                {
                    return Json(new { success = false, message = "Alert not found or not accessible" });
                }

                // Update ALL rows for this recipient within the same group
                var rows = await _db.Alerte
                    .Where(a => a.AlertGroupId == target.AlertGroupId && a.DestinataireUserId == target.DestinataireUserId)
                    .ToListAsync();

                foreach (var r in rows)
                {
                    if (r.EtatAlerteId != 2)
                    {
                        r.EtatAlerteId = 2; // Lu / Confirmé
                        r.DateLecture = DateTime.UtcNow;
                    }
                }
                await _db.SaveChangesAsync();

                // Broadcast to recipient and sender via ReceiveNotification handler used by client
                try
                {
                    if (target.DestinataireUserId.HasValue)
                    {
                        await _hub.Clients.Group($"user_{target.DestinataireUserId.Value}")
                            .SendAsync("ReceiveNotification", "AlertStatusUpdated", new { groupId = target.AlertGroupId, userId = target.DestinataireUserId.Value, status = "Lu", readAt = DateTime.UtcNow });
                        await _hub.Clients.Group($"user_{target.DestinataireUserId.Value}")
                            .SendAsync("ReceiveNotification", "UpdateKpis", null);
                    }
                    if (target.ExpediteurId.HasValue)
                    {
                        await _hub.Clients.Group($"user_{target.ExpediteurId.Value}")
                            .SendAsync("ReceiveNotification", "AlertStatusUpdated", new { groupId = target.AlertGroupId, userId = target.DestinataireUserId, status = "Lu", readAt = DateTime.UtcNow });
                        await _hub.Clients.Group($"user_{target.ExpediteurId.Value}")
                            .SendAsync("ReceiveNotification", "UpdateKpis", null);
                    }
                }
                catch { }

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

                // Fetch all rows then collapse by preferred key:
                // 1) If any row has DestinataireUserId, group by that id
                // 2) Else group by DestinataireEmail or DestinatairePhoneNumber
                var rows = await _db.Alerte
                    .Include(a => a.DestinataireUser)
                    .Where(a => a.AlertGroupId == group)
                    .ToListAsync();

                // Build best-effort resolver so that email/phone rows without DestinataireUserId
                // collapse under the same user if we can resolve a match.
                static string NormalizeEmail(string? e) => (e ?? string.Empty).Trim().ToLowerInvariant();
                static string NormalizePhone(string? p)
                {
                    if (string.IsNullOrWhiteSpace(p)) return string.Empty;
                    var digits = new string(p.Where(char.IsDigit).ToArray());
                    if (digits.StartsWith("00")) digits = digits.Substring(2);
                    if (digits.Length == 8) digits = "216" + digits; // local TN fallback
                    return digits;
                }
                var defUsers = await _db.DefUtilisateurs
                    .Select(u => new { u.util_id, u.util_email, u.grh_emp_id })
                    .ToListAsync();
                var emailToUser = defUsers
                    .Where(u => !string.IsNullOrWhiteSpace(u.util_email))
                    .GroupBy(u => NormalizeEmail(u.util_email))
                    .ToDictionary(g => g.Key, g => g.Select(x => x.util_id).FirstOrDefault());
                // Try GRH phone mapping (best-effort)
                var grhPhones = new Dictionary<decimal, string>();
                try
                {
                    var conn = _db.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT CAST(e.grh_emp_id AS decimal(18,2)), NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_gsm,''))), '') FROM grh_employe e";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(1)) grhPhones[reader.GetDecimal(0)] = reader.GetString(1);
                    }
                }
                catch { }
                try
                {
                    var conn2 = _db.Database.GetDbConnection();
                    if (conn2.State != System.Data.ConnectionState.Open) await conn2.OpenAsync();
                    using var cmd2 = conn2.CreateCommand();
                    cmd2.CommandText = @"SELECT CAST(e.grh_emp_id AS decimal(18,2)), NULLIF(LTRIM(RTRIM(ISNULL(e.grh_emp_gsm,''))), '') FROM grh_employee e";
                    using var reader2 = await cmd2.ExecuteReaderAsync();
                    while (await reader2.ReadAsync())
                    {
                        if (!reader2.IsDBNull(1)) grhPhones[reader2.GetDecimal(0)] = reader2.GetString(1);
                    }
                }
                catch { }
                var phoneToUser = new Dictionary<string, decimal>();
                foreach (var u in defUsers)
                {
                    if (u.grh_emp_id.HasValue && grhPhones.TryGetValue(u.grh_emp_id.Value, out var ph))
                    {
                        var key = NormalizePhone(ph);
                        if (!string.IsNullOrEmpty(key) && !phoneToUser.ContainsKey(key)) phoneToUser[key] = u.util_id;
                    }
                }

                var recipients = rows
                    .GroupBy(a =>
                    {
                        if (a.DestinataireUserId.HasValue && a.DestinataireUserId.Value > 0)
                        {
                            var uidInt = Convert.ToInt32(a.DestinataireUserId.Value);
                            return $"U:{uidInt}";
                        }
                        // Try resolving to a known user via email/phone to collapse under the same recipient
                        var em = NormalizeEmail(a.DestinataireEmail);
                        if (!string.IsNullOrEmpty(em) && emailToUser.TryGetValue(em, out var emUid) && emUid > 0)
                        {
                            var uidInt = Convert.ToInt32(emUid);
                            return $"U:{uidInt}";
                        }
                        var ph = NormalizePhone(a.DestinatairePhoneNumber);
                        if (!string.IsNullOrEmpty(ph) && phoneToUser.TryGetValue(ph, out var phUid) && phUid > 0)
                        {
                            var uidInt = Convert.ToInt32(phUid);
                            return $"U:{uidInt}";
                        }
                        // Fallback: keep as standalone external recipient (email or phone)
                        if (!string.IsNullOrWhiteSpace(a.DestinataireEmail)) return $"E:{NormalizeEmail(a.DestinataireEmail)}";
                        if (!string.IsNullOrWhiteSpace(a.DestinatairePhoneNumber)) return $"P:{NormalizePhone(a.DestinatairePhoneNumber)}";
                        return $"X:{Guid.NewGuid()}";
                    })
                    .Select(g =>
                    {
                        var any = g.First();
                        var hadUser = g.Any(x => x.DestinataireUserId.HasValue);
                        var userId = hadUser ? g.Where(x => x.DestinataireUserId.HasValue).Select(x => x.DestinataireUserId).FirstOrDefault() : null;
                        var userName = hadUser
                            ? rows.Where(x => x.DestinataireUserId == userId).Select(x => x.DestinataireUser != null ? (x.DestinataireUser.util_prenom + " " + x.DestinataireUser.util_nom).Trim() : null).FirstOrDefault()
                            : null;
                        return new
                        {
                            recipientUserId = userId,
                            recipientName = userName,
                            recipientEmail = hadUser ? null : g.Where(x => !string.IsNullOrEmpty(x.DestinataireEmail)).Select(x => x.DestinataireEmail).FirstOrDefault(),
                            recipientPhone = hadUser ? null : g.Where(x => !string.IsNullOrEmpty(x.DestinatairePhoneNumber)).Select(x => x.DestinatairePhoneNumber).FirstOrDefault(),
                            isRead = g.Any(x => x.EtatAlerteId == 2),
                            readDate = g.Where(x => x.EtatAlerteId == 2 && x.DateLecture != null).OrderBy(x => x.DateLecture).Select(x => x.DateLecture).FirstOrDefault(),
                            status = g.Any(x => x.EtatAlerteId == 2) ? "Lu" : "Non Lu"
                        };
                    })
                    .ToList();

                return Json(new { recipients });
            }
            catch (Exception ex)
            {
                return Json(new { recipients = new object[0], error = ex.Message });
            }
        }
    }
}

