using System.Security.Claims;
using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Controllers
{
    [Authorize]
    public sealed class AlertsCrudController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AlertsCrudController(ApplicationDbContext db){ _db = db; }

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
                .Where(u => u.DepartmentId == depId.Value && u.UserId != CurrentUserId)
                .OrderBy(u => u.Username)
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
        public async Task<IActionResult> Send([FromForm] int alertId, [FromForm] string? recipients)
        {
            var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.AlertId == alertId);
            if (alert == null) return NotFound();

            int? depId = CurrentDepartmentId ?? alert.DepartmentId;
            if (!depId.HasValue) return BadRequest("Department required");
            // Scope: SuperUser can only send within own department
            if (CurrentRole.Equals("SuperUser", StringComparison.OrdinalIgnoreCase) && depId != CurrentDepartmentId) return Forbid();

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

            foreach (var uid in targetIds)
            {
                if (!await _db.AlertRecipients.AnyAsync(r => r.AlertId == alert.AlertId && r.UserId == uid))
                {
                    _db.AlertRecipients.Add(new AlertRecipient { AlertId = alert.AlertId, UserId = uid, IsRead = false });
                }
            }
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var a = await _db.Alerts.AsNoTracking().FirstOrDefaultAsync(x => x.AlertId == id);
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

