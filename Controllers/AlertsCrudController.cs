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
using AlertSystem.Services;
using System.Text.Json;

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
        private readonly AlertCancellationService _cancellationService;
        private readonly INotificationService _notificationService;
        private readonly AlertAuditService _auditService;
        private readonly IWhatsAppService _whatsAppService;
        public AlertsCrudController(ApplicationDbContext db, IHubContext<NotificationsHub> hub, IConfiguration cfg, AlertCancellationService cancellationService, INotificationService notificationService, AlertAuditService auditService, IWhatsAppService whatsAppService, AlertSystem.Services.IEmailSender? email = null, ILogger<AlertsCrudController>? logger = null)
        {
            _db = db; _hub = hub; _cfg = cfg; _email = email; _logger = logger; _cancellationService = cancellationService; _notificationService = notificationService; _auditService = auditService; _whatsAppService = whatsAppService;
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
            var q = _db.Alerts.AsNoTracking().Include(a => a.Department).AsQueryable();
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(a => a.AlertType == type);
            if (departmentId.HasValue) q = q.Where(a => a.DepartmentId == departmentId.Value);
            if (from.HasValue) q = q.Where(a => a.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(a => a.CreatedAt < to.Value.AddDays(1));

            // Scope par rôle
            if (CurrentRole.Equals("SuperUser", StringComparison.OrdinalIgnoreCase) && CurrentDepartmentId.HasValue)
                q = q.Where(a => a.DepartmentId == CurrentDepartmentId);
            if (CurrentRole.Equals("User", StringComparison.OrdinalIgnoreCase))
                q = q.Where(a => a.CreatedBy == CurrentUserId);

            var items = await q.OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync();

            // Return JSON for AJAX requests
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                var jsonItems = items.Select(a => new {
                    a.AlertId,
                    a.Title,
                    a.AlertType,
                    a.CreatedAt,
                    DepartmentName = a.Department?.Name
                }).ToList();
                return Json(new { items = jsonItems });
            }

            ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
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
            // Admin peut voir tous les utilisateurs de tous les départements
            if (CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var allUsers = await _db.Users.AsNoTracking()
                    .Where(u => u.UserId != CurrentUserId)
                    .OrderBy(u => u.Role == "Admin" ? 0 : u.Role == "SuperUser" ? 1 : 2) // Admins, puis SuperUsers, puis Users
                    .ThenBy(u => u.Username)
                    .Select(u => new { u.UserId, u.Username, u.Email, u.DepartmentId })
                    .ToListAsync();
                return Json(allUsers);
            }
            
            // SuperUser/User: seulement leur département
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
            // Admin peut créer des alertes sans département spécifique si departmentId n'est pas fourni
            if (!depToUse.HasValue && !CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)) 
                return BadRequest("Department required");
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
            
            // QuickSave ne doit PAS envoyer de notifications - c'est juste pour sauvegarder comme template
            _logger?.LogInformation("=== QUICKSAVE EXECUTED === Alert template saved: {AlertId} - {Title} - NO NOTIFICATIONS SENT", a.AlertId, a.Title);
            
            return Ok(new { a.AlertId });
        }

        // Send alert to selected recipients (within same department). If none provided, send to all except sender.
        [HttpPost]
        public async Task<IActionResult> Send([FromForm] int alertId, [FromForm] string? recipients, [FromForm] string? platforms, [FromForm] string? title, [FromForm] string? message, [FromForm] string? alertType, [FromForm] int? departmentId)
        {
            var template = await _db.Alerts.AsNoTracking().FirstOrDefaultAsync(a => a.AlertId == alertId);
            if (template == null) return NotFound();

            int? depId = CurrentDepartmentId ?? template.DepartmentId; // conservé pour traçabilité
            // Tous les rôles peuvent envoyer à tous les utilisateurs (plus de restriction de département)

            _logger?.LogInformation("=== SEND ALERT START === User: {UserId}, Template: {TemplateId}, Recipients: {Recipients}, Platforms(csv): {PlatformsCsv}, Dept: {DeptId}", 
                CurrentUserId, alertId, recipients, platforms, departmentId);
            try
            {
                var formKeys = string.Join(", ", Request.Form.Keys.ToArray());
                _logger?.LogDebug("Send Form Keys: [{Keys}]", formKeys);
                foreach (var k in Request.Form.Keys)
                {
                    if (k.Equals("platforms", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("Form platforms value(s): {Vals}", string.Join("|", Request.Form[k].ToArray()));
                    }
                    if (k.StartsWith("platform", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("Flag {Key} = {Val}", k, Request.Form[k].ToString());
                    }
                }
            }
            catch {}
                
            // Parser les plateformes sélectionnées à partir de plusieurs formes possibles
            var selectedPlatforms = new List<string>();
            // 1) CSV
            if (!string.IsNullOrEmpty(platforms))
            {
                selectedPlatforms.AddRange(platforms.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim().ToLower()));
            }
            // 2) Répétées: platforms=value1&platforms=value2
            var repeated = Request.Form["platforms"]; // peut contenir plusieurs valeurs
            if (repeated.Count > 1)
            {
                selectedPlatforms.AddRange(repeated.Select(v => v.ToString().Trim().ToLower()));
            }
            // 3) Flags individuels: platformEmail=on, platformWhatsApp=on, platformDesktop=on
            void AddFlag(string key, string value, string normalized)
            {
                var v = Request.Form[key].ToString();
                if (!string.IsNullOrWhiteSpace(v) && !string.Equals(v, "false", StringComparison.OrdinalIgnoreCase))
                {
                    selectedPlatforms.Add(normalized);
                }
            }
            AddFlag("platformEmail", "on", "email");
            AddFlag("platformWhatsApp", "on", "whatsapp");
            AddFlag("platformDesktop", "on", "desktop");
            // Normaliser/filtrer
            selectedPlatforms = selectedPlatforms
                .Select(p => p.Trim().ToLower())
                .Where(p => p == "email" || p == "whatsapp" || p == "desktop")
                .Distinct()
                .ToList();
            
            // Si aucune plateforme n'est sélectionnée, utiliser email et desktop par défaut
            if (selectedPlatforms.Count == 0)
            {
                selectedPlatforms = new List<string> { "email", "desktop" };
            }
            
            _logger?.LogInformation("Selected platforms: [{Platforms}]", string.Join(", ", selectedPlatforms));
                
            // Créer une nouvelle alerte d'envoi (ne pas écraser le template)
            var sendAlert = new Alert
            {
                Title = !string.IsNullOrWhiteSpace(title) ? title! : template.Title,
                Message = !string.IsNullOrWhiteSpace(message) ? message! : template.Message,
                AlertType = !string.IsNullOrWhiteSpace(alertType) ? alertType! : template.AlertType,
                IsManual = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId,
                DepartmentId = CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) 
                    ? (departmentId ?? depId) // Admin peut choisir le département ou utiliser celui du template
                    : depId
            };
            _db.Alerts.Add(sendAlert);
            await _db.SaveChangesAsync();
            
            _logger?.LogInformation("Alert created with ID: {AlertId}", sendAlert.AlertId);

            List<int> targetIds;
            if (!string.IsNullOrWhiteSpace(recipients))
            {
                targetIds = recipients.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s, out var x) ? x : 0)
                    .Where(x => x > 0)
                    .ToList();
                // Valider contre la base pour ignorer des ids inexistants
                var validIds = await _db.Users.AsNoTracking().Where(u => targetIds.Contains(u.UserId))
                    .Select(u => u.UserId).ToListAsync();
                targetIds = validIds;
            }
            else
            {
                // Aucun destinataire spécifié: envoyer à tous les utilisateurs sauf l'expéditeur
                targetIds = await _db.Users.AsNoTracking()
                    .Where(u => u.UserId != CurrentUserId)
                    .Select(u => u.UserId)
                    .ToListAsync();
            }
            // exclude sender
            targetIds = targetIds.Where(id => id != CurrentUserId).ToList();
            
            _logger?.LogInformation("Alert {AlertId}: Found {Count} target recipients: [{Targets}]", 
                sendAlert.AlertId, targetIds.Count, string.Join(", ", targetIds));

            if (targetIds.Count == 0)
            {
                _logger?.LogWarning("Alert {AlertId}: No target recipients found! Returning early.", sendAlert.AlertId);
                return Json(new { ok = true, alertId = sendAlert.AlertId, recipients = 0, emailAttempts = 0, emailSent = 0, 
                    attemptedEmails = new string[0], sentEmails = new string[0], cancellationWindowSeconds = 5 });
            }

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
                    _db.AlertRecipients.Add(new AlertRecipient 
                    { 
                        AlertId = sendAlert.AlertId, 
                        UserId = uid, 
                        IsConfirmed = false,
                        SendStatus = "Pending",
                        DeliveryPlatforms = JsonSerializer.Serialize(selectedPlatforms)
                    });
                }
            }
            await _db.SaveChangesAsync();

            // Envoyer une copie d'information aux admins NON destinataires directs ET qui ne sont pas l'expéditeur
            var adminIds = await _db.Users.AsNoTracking()
                .Where(u => u.Role == "Admin")
                .Select(u => u.UserId)
                .ToListAsync();
            var adminCopyIds = adminIds.Except(targetIds).Where(id => id != CurrentUserId).ToList();
            if (adminCopyIds.Count > 0)
            {
                var adminAlert = new Alert
                {
                    Title = $"[COPIE INFO] {sendAlert.Title}",
                    Message = $"Copie d'information - Alerte envoyée par {await _db.Users.AsNoTracking().Where(u => u.UserId == CurrentUserId).Select(u => u.Username).FirstOrDefaultAsync()}\n\n{sendAlert.Message}",
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
                        _db.AlertRecipients.Add(new AlertRecipient 
                        { 
                            AlertId = adminAlert.AlertId, 
                            UserId = aid, 
                            IsConfirmed = false,
                            SendStatus = "Sent", // Admin copies are sent immediately
                            DeliveryPlatforms = JsonSerializer.Serialize(new[] { "Internal" })
                        });
                    }
                }
                await _db.SaveChangesAsync();
            }

            // Note: L'expéditeur verra ses alertes envoyées dans l'onglet "Envoyées" via SentData()
            // qui filtre par a.CreatedBy == userId. Pas besoin d'AlertRecipient pour l'expéditeur.

                // Push temps réel: notifier destinataires (UI refresh), pas d'envoi immédiat des plateformes ici
            _logger?.LogInformation("Sending alert {AlertId} to {Count} recipients in department {DepartmentId}", sendAlert.AlertId, targetIds.Count, depId);

            foreach (var uid in targetIds)
            {
                await _hub.Clients.User(uid.ToString()).SendAsync("historyChanged");
                await _hub.Clients.User(uid.ToString()).SendAsync("badgeChanged");

                // Marquer les stats email côté UI (tentatives) sans envoyer ici
                var user = await _db.Users.AsNoTracking().Where(u => u.UserId == uid).FirstOrDefaultAsync();
                if (user != null && selectedPlatforms.Contains("email") && !string.IsNullOrWhiteSpace(user.Email))
                {
                    emailAttempts++;
                    attemptedEmails.Add(user.Email);
                    sentEmails.Add(user.Email); // affichage immédiat
                }
            }
            
            // Notify sender immediately that they have a new sent alert
            await _hub.Clients.User(CurrentUserId.ToString()).SendAsync("sentChanged");
            
            // Schedule alert sending with cancellation window
            _ = Task.Run(async () =>
            {
                await _cancellationService.ScheduleAlertSending(sendAlert.AlertId);
                // Notify after sending is complete
                await _hub.Clients.All.SendAsync("deptChanged");
            });

            return Json(new { 
                ok = true, 
                alertId = sendAlert.AlertId,
                recipients = targetIds.Count, 
                emailAttempts, 
                emailSent, 
                attemptedEmails, 
                sentEmails,
                cancellationWindowSeconds = 5
            });
        }

        [HttpPost]
        public async Task<IActionResult> CancelAlert([FromBody] int alertId)
        {
            var success = _cancellationService.CancelAlert(alertId);
            return Json(new { success });
        }

        [HttpGet]
        public IActionResult IsAlertPending(int alertId)
        {
            var isPending = _cancellationService.IsAlertPending(alertId);
            return Json(new { isPending });
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
        public async Task<IActionResult> GetAlertDetails(int id)
        {
            try
            {
                var a = await _db.Alerts.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.AlertId == id);
                    
                if (a == null || !CanSee(a)) 
                {
                    return NotFound(new { error = "Alerte introuvable" });
                }
                
                return Json(new { 
                    title = a.Title, 
                    message = a.Message, 
                    alertType = a.AlertType 
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting alert details for ID {AlertId}", id);
                return BadRequest(new { error = "Erreur lors du chargement" });
            }
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
            
            // Notify all clients of new alert
            await _hub.Clients.All.SendAsync("newAlert");
            await _hub.Clients.All.SendAsync("alertsChanged");
            
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
            
            // Notify all clients of alert update
            await _hub.Clients.All.SendAsync("alertsChanged");
            
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
            
            // Notify all clients of alert deletion
            await _hub.Clients.All.SendAsync("alertsChanged");
            
            return RedirectToAction(nameof(Index));
        }

        // Test endpoint pour WhatsApp
        [HttpPost]
        public async Task<IActionResult> TestWhatsApp([FromForm] string phoneNumber, [FromForm] string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(message))
                {
                    return BadRequest(new { error = "Phone number and message required" });
                }

                _logger?.LogInformation("=== WHATSAPP TEST === Phone: {Phone}, Message: {Message}", phoneNumber, message);
                
                var success = await _whatsAppService.SendMessageAsync(phoneNumber, message);
                
                return Json(new { 
                    success = success, 
                    phoneNumber = phoneNumber,
                    message = success ? "WhatsApp sent successfully!" : "Failed to send WhatsApp",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in WhatsApp test");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Diagnostic avancé WhatsApp: effectue des appels bruts texte + template et renvoie les réponses détaillées
        [HttpPost]
        public async Task<IActionResult> TestWhatsAppDiag([FromForm] string phoneNumber, [FromForm] string? message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return BadRequest(new { error = "Phone number required" });
                }

                var token = _cfg["WhatsApp:AccessToken"] ?? string.Empty;
                var phoneId = _cfg["WhatsApp:PhoneNumberId"] ?? string.Empty;
                var apiVersion = _cfg["WhatsApp:ApiVersion"] ?? "v22.0";
                var tokenTail = token.Length >= 6 ? token[^6..] : token;
                var baseUrl = $"https://graph.facebook.com/{apiVersion}/{phoneId}/messages";

                // Construire payloads
                var textPayload = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "text",
                    text = new { body = string.IsNullOrWhiteSpace(message) ? "Diagnostic ping" : message }
                };
                var templatePayload = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "template",
                    template = new { name = "hello_world", language = new { code = "en_US" } }
                };

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                // Appel texte
                var textContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(textPayload), System.Text.Encoding.UTF8, "application/json");
                var textResp = await client.PostAsync(baseUrl, textContent);
                var textBody = await textResp.Content.ReadAsStringAsync();

                // Appel template
                var tplContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(templatePayload), System.Text.Encoding.UTF8, "application/json");
                var tplResp = await client.PostAsync(baseUrl, tplContent);
                var tplBody = await tplResp.Content.ReadAsStringAsync();

                _logger?.LogInformation("WA DIAG cfg: api={Api} phoneId={PhoneId} tokenLen={Len} tokenTail={Tail}", apiVersion, phoneId, token.Length, tokenTail);
                _logger?.LogInformation("WA DIAG text -> {Status} {Body}", textResp.StatusCode, textBody);
                _logger?.LogInformation("WA DIAG template -> {Status} {Body}", tplResp.StatusCode, tplBody);

                return Json(new
                {
                    config = new { apiVersion, phoneId, tokenLen = token.Length, tokenTail },
                    requests = new { baseUrl, phoneNumber },
                    text = new { status = (int)textResp.StatusCode, ok = textResp.IsSuccessStatusCode, body = textBody },
                    template = new { status = (int)tplResp.StatusCode, ok = tplResp.IsSuccessStatusCode, body = tplBody }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in WhatsApp diagnostics");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int alertId)
        {
            try
            {
                // Find all alert recipients for this alert
                var recipients = await _db.AlertRecipients
                    .Where(ar => ar.AlertId == alertId)
                    .ToListAsync();

                if (!recipients.Any())
                {
                    return Json(new { success = false, message = "Alerte non trouvée" });
                }

                // Check if the current user is the sender
                var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.AlertId == alertId);
                if (alert == null || alert.CreatedBy != CurrentUserId)
                {
                    return Json(new { success = false, message = "Vous n'êtes pas autorisé à annuler cette alerte" });
                }

                // Check if cancellation is still possible (within 10 seconds)
                var timeSinceCreation = DateTime.UtcNow - alert.CreatedAt;
                if (timeSinceCreation.TotalSeconds > 10)
                {
                    return Json(new { success = false, message = "La période d'annulation est expirée" });
                }

                // Update all recipients to cancelled status
                foreach (var recipient in recipients)
                {
                    recipient.SendStatus = "Cancelled";
                }

                await _db.SaveChangesAsync();

                // Notify all clients of the cancellation
                await _hub.Clients.All.SendAsync("alertStatusChanged", alertId, "Cancelled");

                _logger?.LogInformation("Alert {AlertId} cancelled by user {UserId}", alertId, CurrentUserId);

                return Json(new { success = true, message = "Alerte annulée avec succès" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error cancelling alert {AlertId}", alertId);
                return Json(new { success = false, message = "Erreur lors de l'annulation" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Recipients()
        {
            try
            {
                var users = await _db.Users.AsNoTracking()
                    .Include(u => u.Department)
                    .Select(u => new { 
                        u.UserId, 
                        u.Username, 
                        u.Email, 
                        u.Role, 
                        DepartmentName = u.Department != null ? u.Department.Name : null, 
                        u.DepartmentId 
                    })
                    .OrderBy(u => u.Username)
                    .ToListAsync();
                    
                _logger?.LogInformation("Recipients endpoint: returning {Count} users", users.Count);
                return Json(users);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading recipients");
                return Json(new { error = "Failed to load recipients" });
            }
        }

        // Diagnostic: list users with phone info for WhatsApp eligibility
        [HttpGet]
        public async Task<IActionResult> RecipientsPhones()
        {
            try
            {
                var users = await _db.Users.AsNoTracking()
                    .OrderBy(u => u.UserId)
                    .Select(u => new {
                        u.UserId,
                        u.Username,
                        u.Email,
                        u.Role,
                        u.DepartmentId,
                        Phone = u.PhoneNumber,
                        HasPhone = !string.IsNullOrWhiteSpace(u.PhoneNumber)
                    })
                    .ToListAsync();
                return Json(new { count = users.Count, users });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading RecipientsPhones");
                return Json(new { error = "Failed to load RecipientsPhones" });
            }
        }
    }
}

