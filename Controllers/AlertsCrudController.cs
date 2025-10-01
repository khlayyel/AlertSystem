using System.Security.Claims;
using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using AlertSystem.Hubs;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Controllers
{
    [Authorize]
    public sealed class AlertsCrudController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<NotificationsHub> _hub;
        private readonly IConfiguration _cfg;
        private readonly PushServiceClient? _pushClient;
        private readonly AlertSystem.Services.IEmailSender? _email;
        private readonly ILogger<AlertsCrudController>? _logger;
        public AlertsCrudController(ApplicationDbContext db, IHubContext<NotificationsHub> hub, IConfiguration cfg, AlertSystem.Services.IEmailSender? email = null, ILogger<AlertsCrudController>? logger = null)
        {
            _db = db; _hub = hub; _cfg = cfg; _email = email; _logger = logger;
            var pub = _cfg["WebPush:PublicKey"];
            var priv = _cfg["WebPush:PrivateKey"];
            var subject = _cfg["WebPush:Subject"];
            if (!string.IsNullOrWhiteSpace(pub) && !string.IsNullOrWhiteSpace(priv))
            {
                _pushClient = new PushServiceClient
                {
                    DefaultAuthentication = new VapidAuthentication(pub, priv) { Subject = subject }
                };
                _logger?.LogInformation("WebPush client initialized with VAPID subject {Subject}", subject);
            }
            else
            {
                _logger?.LogWarning("WebPush keys missing; push will be skipped.");
            }

            if (_email == null)
            {
                // Fallback: build a sender directly so emails are not skipped
                _logger?.LogWarning("IEmailSender not resolved by DI; using fallback SmtpEmailSender.");
                _email = new AlertSystem.Services.SmtpEmailSender(_cfg, LoggerFactory.Create(b => b.AddConsole()).CreateLogger<AlertSystem.Services.SmtpEmailSender>());
            }
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        private int? CurrentDepartmentId
        {
            get { var dep = User.FindFirst("department")?.Value; return int.TryParse(dep, out var id) ? id : (int?)null; }
        }

        private bool CanSee(Alert a)
        {
            if (CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return true;
            if (CurrentRole.Equals("SuperUser", StringComparison.OrdinalIgnoreCase)) return a.DepartmentId == CurrentDepartmentId;
            return a.CreatedBy == CurrentUserId;
        }

        private void GuardEdit(Alert a)
        {
            if (CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return;
            if (CurrentRole.Equals("SuperUser", StringComparison.OrdinalIgnoreCase))
            {
                if (a.DepartmentId != CurrentDepartmentId) throw new UnauthorizedAccessException();
                return;
            }
            if (a.CreatedBy != CurrentUserId) throw new UnauthorizedAccessException();
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? type, int? departmentId, DateTime? from, DateTime? to)
        {
            var q = _db.Alerts.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(a => a.AlertType == type);
            if (departmentId.HasValue) q = q.Where(a => a.DepartmentId == departmentId.Value);
            if (from.HasValue) q = q.Where(a => a.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(a => a.CreatedAt < to.Value.AddDays(1));

            // Scope par rôle
            if (CurrentRole.Equals("SuperUser", StringComparison.OrdinalIgnoreCase) && CurrentDepartmentId.HasValue)
                q = q.Where(a => a.DepartmentId == CurrentDepartmentId);
            if (CurrentRole.Equals("User", StringComparison.OrdinalIgnoreCase))
                q = q.Where(a => a.CreatedBy == CurrentUserId);

            ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            var items = await q.OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync();
            return View(items);
        }

        // Quick alerts list (presets) for the current user
        [HttpGet]
        public async Task<IActionResult> QuickList()
        {
            var uid = CurrentUserId;
            var items = await _db.Alerts.AsNoTracking()
                .Where(a => a.CreatedBy == uid)
                .OrderByDescending(a => a.CreatedAt)
                .Take(20)
                .Select(a => new { a.AlertId, a.Title, a.Message, a.AlertType, a.DepartmentId })
                .ToListAsync();
            return Json(items);
        }

        // Users of current department (for recipient selection)
        [HttpGet]
        public async Task<IActionResult> DeptUsers()
        {
            var depId = CurrentDepartmentId;
            if (!depId.HasValue) return Json(Array.Empty<object>());
            var users = await _db.Users.AsNoTracking()
                .Where(u => (u.DepartmentId == depId.Value || u.Role == "Admin") && u.UserId != CurrentUserId)
                .OrderByDescending(u => u.Role == "Admin") // admins en haut
                .ThenBy(u => u.Username)
                .Select(u => new { u.UserId, u.Username, u.Email })
                .ToListAsync();
            return Json(users);
        }

        // Save current form as quick alert (template)
        [HttpPost]
        public async Task<IActionResult> QuickSave([FromForm] string title, [FromForm] string message, [FromForm] string alertType, [FromForm] int? departmentId)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(alertType)) return BadRequest();
            int? depToUse = CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? departmentId : CurrentDepartmentId;
            if (!depToUse.HasValue) return BadRequest("Department required");
            var a = new Alert
            {
                Title = title,
                Message = message,
                AlertType = alertType,
                IsManual = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId,
                DepartmentId = depToUse
            };
            _db.Alerts.Add(a);
            await _db.SaveChangesAsync();
            return Ok(new { a.AlertId });
        }

        // Send alert to selected recipients (within same department). If none provided, send to all except sender.
        [HttpPost]
        public async Task<IActionResult> Send([FromForm] int alertId, [FromForm] string? recipients, [FromForm] string? title, [FromForm] string? message, [FromForm] string? alertType, [FromForm] int? departmentId)
        {
            var template = await _db.Alerts.AsNoTracking().FirstOrDefaultAsync(a => a.AlertId == alertId);
            if (template == null) return NotFound();

            int? depId = CurrentDepartmentId ?? template.DepartmentId;
            if (!depId.HasValue) return BadRequest("Department required");
            // Scope: SuperUser can only send within own department
            if (CurrentRole.Equals("SuperUser", StringComparison.OrdinalIgnoreCase) && depId != CurrentDepartmentId) return Forbid();

            // Créer une nouvelle alerte d'envoi (ne pas écraser le template)
            var sendAlert = new Alert
            {
                Title = !string.IsNullOrWhiteSpace(title) ? title! : template.Title,
                Message = !string.IsNullOrWhiteSpace(message) ? message! : template.Message,
                AlertType = !string.IsNullOrWhiteSpace(alertType) ? alertType! : template.AlertType,
                IsManual = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId,
                DepartmentId = (CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) && departmentId.HasValue)
                    ? departmentId.Value
                    : depId
            };
            _db.Alerts.Add(sendAlert);
            await _db.SaveChangesAsync();

            List<int> targetIds;
            if (!string.IsNullOrWhiteSpace(recipients))
            {
                targetIds = recipients.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s, out var x) ? x : 0)
                    .Where(x => x > 0)
                    .ToList();
                // keep only users in department
                var deptIds = await _db.Users.AsNoTracking().Where(u => u.DepartmentId == depId.Value && targetIds.Contains(u.UserId))
                    .Select(u => u.UserId).ToListAsync();
                targetIds = deptIds;
            }
            else
            {
                targetIds = await _db.Users.AsNoTracking()
                    .Where(u => u.DepartmentId == depId)
                    .Select(u => u.UserId)
                    .ToListAsync();
            }
            // exclude sender
            targetIds = targetIds.Where(id => id != CurrentUserId).ToList();

            // Resolve sender display name once
            var senderName = await _db.Users.AsNoTracking().Where(u => u.UserId == CurrentUserId).Select(u => u.Username).FirstOrDefaultAsync() ?? $"User#{CurrentUserId}";
            var detailsUrl = $"{Request.Scheme}://{Request.Host}/AlertsCrud/Details/{sendAlert.AlertId}";

            int emailAttempts = 0;
            int emailSent = 0;
            var attemptedEmails = new List<string>();
            var sentEmails = new List<string>();
            foreach (var uid in targetIds)
            {
                if (!await _db.AlertRecipients.AnyAsync(r => r.AlertId == sendAlert.AlertId && r.UserId == uid))
                {
                    _db.AlertRecipients.Add(new AlertRecipient { AlertId = sendAlert.AlertId, UserId = uid, IsRead = false });
                }
            }
            await _db.SaveChangesAsync();

            // Envoyer une copie d'information aux admins NON destinataires directs
            var adminIds = await _db.Users.AsNoTracking()
                .Where(u => u.Role == "Admin")
                .Select(u => u.UserId)
                .ToListAsync();
            var adminCopyIds = adminIds.Except(targetIds).ToList();
            if (adminCopyIds.Count > 0)
            {
                var adminAlert = new Alert
                {
                    Title = sendAlert.Title,
                    Message = sendAlert.Message,
                    AlertType = "Information",
                    IsManual = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = CurrentUserId,
                    DepartmentId = depId
                };
                _db.Alerts.Add(adminAlert);
                await _db.SaveChangesAsync();
                foreach (var aid in adminCopyIds)
                {
                    if (!await _db.AlertRecipients.AnyAsync(r => r.AlertId == adminAlert.AlertId && r.UserId == aid))
                    {
                        _db.AlertRecipients.Add(new AlertRecipient { AlertId = adminAlert.AlertId, UserId = aid, IsRead = false });
                    }
                }
                await _db.SaveChangesAsync();
            }

            // Push temps réel: notifier destinataires, expéditeur et vues département
            _logger?.LogInformation("Sending alert {AlertId} to {Count} recipients in department {DepartmentId}", sendAlert.AlertId, targetIds.Count, depId);

            foreach (var uid in targetIds)
            {
                await _hub.Clients.User(uid.ToString()).SendAsync("historyChanged");
                await _hub.Clients.User(uid.ToString()).SendAsync("badgeChanged");
                await _hub.Clients.User(uid.ToString()).SendAsync("newAlert", new {
                    title = sendAlert.Title,
                    message = sendAlert.Message,
                    alertType = sendAlert.AlertType,
                    alertId = sendAlert.AlertId
                });

                // Send Web Push (to recipients only)
                if (_pushClient != null)
                {
                    var subs = await _db.WebPushSubscriptions.AsNoTracking().Where(s => s.UserId == uid).ToListAsync();
                    foreach (var s in subs)
                    {
                        try
                        {
                            var subscription = new PushSubscription
                            {
                                Endpoint = s.Endpoint,
                                Keys = new Dictionary<string, string>{{"p256dh", s.P256dh},{"auth", s.Auth}}
                            };
                            var payload = System.Text.Json.JsonSerializer.Serialize(new { title = sendAlert.Title, message = sendAlert.Message, url = $"/AlertsCrud/Details/{sendAlert.AlertId}" });
                            await _pushClient.RequestPushMessageDeliveryAsync(subscription, new PushMessage(payload) { Topic = "alert" });
                            _logger?.LogInformation("WebPush sent to user {UserId}", uid);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "WebPush failed for user {UserId}", uid);
                        }
                    }
                }

                // Email notification (best-effort)
                if (_email != null)
                {
                    try
                    {
                        var email = await _db.Users.AsNoTracking().Where(u => u.UserId == uid).Select(u => u.Email).FirstOrDefaultAsync();
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            emailAttempts++;
                            attemptedEmails.Add(email);
                            var body = $"Titre: {sendAlert.Title}\n"+
                                       $"Type: {sendAlert.AlertType}\n"+
                                       $"Envoyée par: {senderName}\n"+
                                       $"Département: {(sendAlert.DepartmentId?.ToString() ?? "-")}\n"+
                                       $"Date: {sendAlert.CreatedAt:yyyy-MM-dd HH:mm}\n\n"+
                                       $"Message:\n{sendAlert.Message}\n\n"+
                                       $"Ouvrir l'alerte: {detailsUrl}";
                            await _email.SendAsync(email, $"[{sendAlert.AlertType}] {sendAlert.Title}", body);
                            _logger?.LogInformation("Email sent to {Email} for alert {AlertId}", email, sendAlert.AlertId);
                            emailSent++;
                            sentEmails.Add(email);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Email send failed to user {UserId}", uid);
                    }
                }
            }
            await _hub.Clients.User(CurrentUserId.ToString()).SendAsync("sentChanged");
            await _hub.Clients.All.SendAsync("deptChanged");
            return Json(new { ok = true, recipients = targetIds.Count, emailAttempts, emailSent, attemptedEmails, sentEmails });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var a = await _db.Alerts.AsNoTracking()
                .Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.AlertId == id);
            if (a == null || !CanSee(a)) return NotFound();
            return View(a);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            return View(new Alert{ CreatedAt = DateTime.UtcNow, AlertType = "Information" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Alert model)
        {
            if (string.IsNullOrWhiteSpace(model.Title)) ModelState.AddModelError("Title", "Titre requis");
            if (string.IsNullOrWhiteSpace(model.Message)) ModelState.AddModelError("Message", "Message requis");
            if (string.IsNullOrWhiteSpace(model.AlertType)) ModelState.AddModelError("AlertType", "Type requis");

            // Forcer CreatedBy et Department selon rôle
            model.CreatedBy = CurrentUserId;
            if (CurrentRole.Equals("SuperUser", StringComparison.OrdinalIgnoreCase)) model.DepartmentId = CurrentDepartmentId;
            if (CurrentRole.Equals("User", StringComparison.OrdinalIgnoreCase)) model.DepartmentId = CurrentDepartmentId;
            model.CreatedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
                return View(model);
            }
            _db.Alerts.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var a = await _db.Alerts.FindAsync(id);
            if (a == null) return NotFound();
            try { GuardEdit(a); } catch { return Forbid(); }
            ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            return View(a);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Alert model)
        {
            if (id != model.AlertId) return BadRequest();
            var a = await _db.Alerts.FindAsync(id);
            if (a == null) return NotFound();
            try { GuardEdit(a); } catch { return Forbid(); }

            if (string.IsNullOrWhiteSpace(model.Title)) ModelState.AddModelError("Title", "Titre requis");
            if (string.IsNullOrWhiteSpace(model.Message)) ModelState.AddModelError("Message", "Message requis");
            if (string.IsNullOrWhiteSpace(model.AlertType)) ModelState.AddModelError("AlertType", "Type requis");
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
                return View(model);
            }

            // Mise à jour champs autorisés
            a.Title = model.Title;
            a.Message = model.Message;
            a.AlertType = model.AlertType;
            a.IsManual = model.IsManual;
            if (CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)) a.DepartmentId = model.DepartmentId; // seuls admin changent librement

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _db.Alerts.FindAsync(id);
            if (a == null) return NotFound();
            try { GuardEdit(a); } catch { return Forbid(); }
            _db.Alerts.Remove(a);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}

