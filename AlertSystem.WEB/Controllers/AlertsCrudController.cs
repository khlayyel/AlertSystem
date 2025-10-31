using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using AlertSystem.Services;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using AlertSystem.WEB.Services;
using Microsoft.AspNetCore.SignalR;
using AlertSystem.Infrastructure.Hubs;

namespace AlertSystem.WEB.Controllers
{
    [Authorize]
    public sealed class AlertsCrudController : Controller
    {
        private readonly AlertCrudService _service;
        private readonly AlertSendService _sendService;
        private readonly ICurrentUserAccessor _currentUser;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AlertsCrudController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly KpiUpdateService _KpiUpdateService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AlertsCrudController(
            AlertCrudService service, 
            AlertSendService sendService, 
            ICurrentUserAccessor currentUser, 
            ICurrentUserService currentUserService,
            ILogger<AlertsCrudController> logger,
            ApplicationDbContext db,
            KpiUpdateService KpiUpdateService,
            IHubContext<NotificationHub> hubContext)
        {
            _service = service;
            _sendService = sendService;
            _currentUser = currentUser;
            _currentUserService = currentUserService;
            _logger = logger;
            _db = db;
            _KpiUpdateService = KpiUpdateService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> QuickList()
        {
            var quickAlerts = await _service.GetQuickListAsync();
            return Json(quickAlerts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFromTemplate([FromBody] CreateFromTemplateDto dto)
        {
            var (success, alertId, error) = await _service.CreateFromTemplateAsync(dto.Title, dto.Message, dto.Type);
            if (!success) return BadRequest(new { error = error ?? "Failed to create alert" });
            return Json(new { success = true, alertId });
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuick([FromBody] CreateFromTemplateDto dto)
        {
            var (success, alertId, error) = await _service.CreateFromTemplateAsync(dto.Title, dto.Message, dto.Type);
            if (!success) return BadRequest(new { error = error ?? "Failed to save quick alert" });
            return Json(new { success = true, alertId });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Consumes("application/json"), Produces("application/json")]
        public async Task<IActionResult> Send()
        {
            try
            {
                _logger.LogInformation("Send: entered with content-type {CT}, content-length {CL}", Request.ContentType, Request.ContentLength);
                SendDto? dto = null;
                {
                    // Fallback: manually read and deserialize the JSON body to avoid model binding issues
                    try
                    {
                        Request.EnableBuffering(); // allow reading body twice
                        using var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                        var body = await reader.ReadToEndAsync();
                        Request.Body.Position = 0; // reset for later middleware
                        _logger.LogWarning("Send: dto was null, attempting manual parse. Body length={Len} body={Body}", body?.Length ?? 0, body);
                        if (!string.IsNullOrWhiteSpace(body))
                        {
                            dto = System.Text.Json.JsonSerializer.Deserialize<SendDto>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Send: manual JSON parse failed");
                    }

                    if (dto == null)
                    {
                        return BadRequest("Invalid JSON payload");
                    }
                }

                // Normalize
                dto.Platforms ??= new PlatformsDto();
                dto.Emails = dto.Emails ?? Array.Empty<string>();
                dto.Phones = dto.Phones ?? Array.Empty<string>();
                dto.UserIds = dto.UserIds ?? Array.Empty<int>();
                dto.EmailUserMap = dto.EmailUserMap ?? new Dictionary<string, int>();
                dto.PhoneUserMap = dto.PhoneUserMap ?? new Dictionary<string, int>();

                _logger.LogInformation("Send: payload normalized. title='{Title}', msgLen={MsgLen}, emails={Emails}, phones={Phones}, desktop={Desktop}, typeId={TypeId}",
                    dto.Title, (dto.Message?.Length ?? 0), dto.Emails.Length, dto.Phones.Length, dto.Platforms.Desktop, dto.AlertTypeId);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Send: ModelState invalid: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest("Invalid payload");
                }
                if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("title required");
                if (string.IsNullOrWhiteSpace(dto.Message)) dto.Message = string.Empty;

                // Desktop fallback: if Desktop selected and no userIds provided, use current logged-in user
                int[]? userIds = dto.UserIds;
                if ((dto.Platforms?.Desktop ?? false) && (userIds == null || userIds.Length == 0))
                {
                    var uid = _currentUser.GetUserId();
                    if (uid.HasValue && uid.Value > 0)
                    {
                        userIds = new[] { uid.Value };
                    }
                }

                // Get current user ID for ExpediteurId
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                // Helper local functions
                static string NormalizeEmail(string? e) => (e ?? string.Empty).Trim().ToLowerInvariant();
                static string NormalizePhone(string? p)
                {
                    if (string.IsNullOrWhiteSpace(p)) return string.Empty;
                    var digits = new string(p.Where(char.IsDigit).ToArray());
                    if (digits.StartsWith("00")) digits = digits.Substring(2);
                    if (digits.Length == 8) digits = "216" + digits; // assume TN local fallback
                    return digits; // compare as digits only
                }

                // Create alert in database first (with pending status)
                // Generate a new AlertGroupId for this alert group
                var alertGroupId = Guid.NewGuid();
                var alertRecords = new List<AlertSystem.Entities.Entities.Alerte>();

                // Create Alerte entries for each recipient/platform combination

                // Add email recipients
                if (dto.Platforms?.Email == true && dto.Emails != null)
                {
                    // preload users for case-insensitive compare
                    var allUsers = await _db.DefUtilisateurs
                        .Select(u => new { u.util_id, email = u.util_email })
                        .ToListAsync();
                    foreach (var email in dto.Emails)
                    {
                        var norm = NormalizeEmail(email);
                        // 1) Prefer explicit mapping provided by UI when selecting with "+"
                        AlertSystem.Entities.Entities.DefUtilisateur? resolvedUser = null;
                        // exact key
                        if (dto.EmailUserMap.TryGetValue(email, out var mappedUserId))
                        {
                            resolvedUser = await _db.DefUtilisateurs.FirstOrDefaultAsync(u => u.util_id == mappedUserId);
                        }
                        // case-insensitive key search
                        if (resolvedUser == null && dto.EmailUserMap.Count > 0)
                        {
                            var kv = dto.EmailUserMap.FirstOrDefault(k => string.Equals(k.Key?.Trim(), email?.Trim(), StringComparison.OrdinalIgnoreCase));
                            if (!string.IsNullOrEmpty(kv.Key) && kv.Value > 0)
                            {
                                resolvedUser = await _db.DefUtilisateurs.FirstOrDefaultAsync(u => u.util_id == kv.Value);
                            }
                        }
                        // 2) Fallback to DB email match (case-insensitive)
                        var userEntry = allUsers.FirstOrDefault(u => NormalizeEmail(u.email) == norm);
                        var alertRecord = new AlertSystem.Entities.Entities.Alerte
                        {
                            AlertGroupId = alertGroupId,
                            TitreAlerte = dto.Title,
                            DescriptionAlerte = dto.Message,
                            DateCreationAlerte = DateTime.UtcNow,
                            StatutId = 1, // En Cours (pending)
                            AlertTypeId = dto.AlertTypeId ?? 1,
                            EtatAlerteId = 1, // Non Lu
                            PlateformeEnvoieId = 1, // Email
                            ExpediteurId = currentUserId.Value, // Set sender ID
                            DestinataireEmail = email,
                            DestinataireUserId = resolvedUser?.util_id ?? userEntry?.util_id,
                            ProcessedByWorker = false // Let WatcherWorker process this
                        };
                        alertRecords.Add(alertRecord);
                    }
                }

                // Add WhatsApp recipients
                if (dto.Platforms?.WhatsApp == true && dto.Phones != null)
                {
                    // Preload users with possible GRH phone by joining on grh_emp_id best-effort
                    var usersWithEmp = await _db.DefUtilisateurs
                        .Select(u => new { u.util_id, u.grh_emp_id })
                        .ToListAsync();
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
                    // Build phone index
                    var userPhoneIndex = new Dictionary<string, decimal>();
                    foreach (var u in usersWithEmp)
                    {
                        if (u.grh_emp_id.HasValue && grhPhones.TryGetValue(u.grh_emp_id.Value, out var ph))
                        {
                            var key = NormalizePhone(ph);
                            if (!string.IsNullOrEmpty(key) && !userPhoneIndex.ContainsKey(key))
                                userPhoneIndex[key] = u.util_id;
                        }
                    }
                    foreach (var phone in dto.Phones)
                    {
                        // Try map phone to a userId provided by UI, validate existence
                        AlertSystem.Entities.Entities.DefUtilisateur? user = null;
                        if (dto.PhoneUserMap.TryGetValue(phone, out var phoneUserId))
                        {
                            user = await _db.DefUtilisateurs.FirstOrDefaultAsync(u => u.util_id == phoneUserId);
                        }
                        // case-insensitive / normalized match
                        if (user == null && dto.PhoneUserMap.Count > 0)
                        {
                            string NormDigits(string s){ var d=new string((s??string.Empty).Where(char.IsDigit).ToArray()); if (d.StartsWith("00")) d=d.Substring(2); return d; }
                            var target = NormDigits(phone);
                            var kv = dto.PhoneUserMap.FirstOrDefault(k => NormDigits(k.Key) == target);
                            if (!string.IsNullOrEmpty(kv.Key) && kv.Value > 0)
                            {
                                user = await _db.DefUtilisateurs.FirstOrDefaultAsync(u => u.util_id == kv.Value);
                            }
                        }
                        if (user == null)
                        {
                            var key = NormalizePhone(phone);
                            if (userPhoneIndex.TryGetValue(key, out var uid))
                            {
                                user = await _db.DefUtilisateurs.FirstOrDefaultAsync(u => u.util_id == uid);
                            }
                        }
                        var alertRecord = new AlertSystem.Entities.Entities.Alerte
                        {
                            AlertGroupId = alertGroupId,
                            TitreAlerte = dto.Title,
                            DescriptionAlerte = dto.Message,
                            DateCreationAlerte = DateTime.UtcNow,
                            StatutId = 1, // En Cours (pending)
                            AlertTypeId = dto.AlertTypeId ?? 1,
                            EtatAlerteId = 1, // Non Lu
                            PlateformeEnvoieId = 2, // WhatsApp
                            ExpediteurId = currentUserId.Value, // Set sender ID
                            DestinatairePhoneNumber = phone,
                            DestinataireUserId = user?.util_id,
                            ProcessedByWorker = false // Let WatcherWorker process this
                        };
                        alertRecords.Add(alertRecord);
                    }
                }

                // Add desktop recipients
                if (dto.Platforms?.Desktop == true && userIds != null)
                {
                    // validate user ids exist to avoid FK errors
                    // Use in-memory intersection to avoid SQL syntax edge-cases observed on some servers
                    var allIds = await _db.DefUtilisateurs
                        .Select(u => (int)u.util_id)
                        .ToListAsync();
                    var existingIds = userIds.Intersect(allIds).ToList();
                    foreach (var userId in existingIds)
                    {
                        var alertRecord = new AlertSystem.Entities.Entities.Alerte
                        {
                            AlertGroupId = alertGroupId,
                            TitreAlerte = dto.Title,
                            DescriptionAlerte = dto.Message,
                            DateCreationAlerte = DateTime.UtcNow,
                            StatutId = 1, // En Cours (pending)
                            AlertTypeId = dto.AlertTypeId ?? 1,
                            EtatAlerteId = 1, // Non Lu
                            PlateformeEnvoieId = 3, // Desktop
                            ExpediteurId = currentUserId.Value, // Set sender ID
                            DestinataireUserId = (decimal)userId,
                            DestinataireDesktop = null, // DesktopDeviceToken not available in DefUtilisateur
                            ProcessedByWorker = false // Let WatcherWorker process this
                        };
                        alertRecords.Add(alertRecord);
                    }
                }

                if (alertRecords.Any())
                {
                    _db.Alerte.AddRange(alertRecords);
                    await _db.SaveChangesAsync();
                }

                // Alert records are now ready for WatcherWorker to process

                // Send real-time KPI update
                try
                {
                    var currentUserIdForKpi = _currentUser.GetUserId();
                    if (currentUserIdForKpi.HasValue)
                    {
                        await _KpiUpdateService.SendOutboxKpiUpdateAsync(currentUserIdForKpi.Value);
                        // Broadcast KPI refresh to sender
                        await _hubContext.Clients.Group($"user_{currentUserIdForKpi.Value}")
                            .SendAsync("UpdateKpis");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send outbox KPI update after alert creation");
                }

                // Send SignalR notification for real-time updates
                try
                {
                    if (currentUserId.HasValue)
                    {
                        await _hubContext.Clients.Group($"user_{currentUserId.Value}")
                            .SendAsync("ReceiveNotification", "AlertCreated", new { 
                                alertGroupId = alertGroupId,
                                title = dto.Title,
                                message = dto.Message,
                                timestamp = DateTime.UtcNow
                            });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send SignalR notification after alert creation");
                }

                return Ok(new { 
                    success = true, 
                    alertGroupId = alertGroupId,
                    message = "Alert queued for processing by WatcherWorker"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertsCrud/Send failed with exception");
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpPost("CancelSend/{alerteId}")]
        public async Task<IActionResult> CancelSend(int alerteId)
        {
            try
            {
                var alert = await _db.Alerte.FindAsync(alerteId);
                if (alert == null)
                {
                    return NotFound("Alert not found");
                }

                // Update alert status to cancelled for all records in the group
                var alertRecords = await _db.Alerte
                    .Where(a => a.AlertGroupId == alert.AlertGroupId)
                    .ToListAsync();

                foreach (var record in alertRecords)
                {
                    record.StatutId = 3; // Annulé
                }
                await _db.SaveChangesAsync();

                _logger.LogInformation("Alert {AlerteId} send cancelled", alerteId);

                return Ok(new { success = true, message = "Send cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel send for alert {AlerteId}", alerteId);
                return StatusCode(500, ex.Message);
            }
        }

        public sealed class CreateFromTemplateDto
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Type { get; set; } = "acquittementNonNécessaire";
        }

        public sealed class SendDto
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string[]? Emails { get; set; }
            public string[]? Phones { get; set; }
            public int[]? UserIds { get; set; }
            public Dictionary<string,int>? EmailUserMap { get; set; }
            public Dictionary<string,int>? PhoneUserMap { get; set; }
            public PlatformsDto? Platforms { get; set; }
            public int? AlertTypeId { get; set; }
        }

        public sealed class PlatformsDto
        {
            public bool Email { get; set; }
            public bool WhatsApp { get; set; }
            public bool Desktop { get; set; }
        }
    }
}
