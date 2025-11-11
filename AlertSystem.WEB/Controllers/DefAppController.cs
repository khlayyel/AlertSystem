using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;
using Microsoft.AspNetCore.SignalR;
using AlertSystem.Infrastructure.Hubs;

namespace AlertSystem.WEB.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class DefAppController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DefAppController> _logger;
        private readonly IHubContext<NotificationHub>? _hub;

        public DefAppController(ApplicationDbContext db, ILogger<DefAppController> logger, IHubContext<NotificationHub>? hub = null)
        {
            _db = db;
            _logger = logger;
            _hub = hub;
        }

        // GET: DefApp
        public async Task<IActionResult> Index()
        {
            var apps = await _db.DefApp.OrderBy(a => a.AppId).ToListAsync();
            return View(apps);
        }

        // GET: DefApp/list
        [HttpGet("list")]
        public async Task<IActionResult> List(string? q = null, int page = 1, int pageSize = 10)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var qry = _db.DefApp.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var ql = q.Trim().ToLower();
                qry = qry.Where(a => (a.Description ?? "").ToLower().Contains(ql));
            }
            var total = await qry.CountAsync();
            var items = await qry
                .OrderBy(a => a.AppId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new { a.AppId, a.Description })
                .ToListAsync();
            return Json(new { total, items, page, pageSize });
        }

        // GET: DefApp/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DefApp/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AlertSystem.Entities.Entities.DefApp app)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _db.DefApp.Add(app);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Created DefApp {AppId}: {Description}", app.AppId, app.Description);
                    if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefApp", action = "Create", id = app.AppId });
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating DefApp");
                    ModelState.AddModelError("", "Une erreur est survenue lors de la création.");
                }
            }
            return View(app);
        }

        // GET: DefApp/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var app = await _db.DefApp.FindAsync(id);
            if (app == null)
            {
                return NotFound();
            }

            return View(app);
        }

        // POST: DefApp/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AlertSystem.Entities.Entities.DefApp app)
        {
            if (id != app.AppId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _db.Update(app);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Updated DefApp {AppId}: {Description}", app.AppId, app.Description);
                    if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefApp", action = "Edit", id = app.AppId });
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await DefAppExists(app.AppId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating DefApp {AppId}", app.AppId);
                    ModelState.AddModelError("", "Une erreur est survenue lors de la mise à jour.");
                }
            }
            return View(app);
        }

        // GET: DefApp/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var app = await _db.DefApp.FindAsync(id);
            if (app == null)
            {
                return NotFound();
            }

            // Check if app is used by any alerts
            var usedInAlerts = await _db.Alerte.AnyAsync(a => a.AppId == id);
            if (usedInAlerts)
            {
                ViewBag.ErrorMessage = "Cette application ne peut pas être supprimée car elle est utilisée par des alertes.";
            }

            return View(app);
        }

        // POST: DefApp/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var app = await _db.DefApp.FindAsync(id);
            if (app == null)
            {
                return NotFound();
            }

            // Check if app is used
            var usedInAlerts = await _db.Alerte.AnyAsync(a => a.AppId == id);
            var usedInTypeAlertes = await _db.DefTypeAlerte.AnyAsync(t => t.AppId == id);
            var usedInUtilisateurs = await _db.DefUtilisateur.AnyAsync(u => u.AppId == id);

            if (usedInAlerts || usedInTypeAlertes || usedInUtilisateurs)
            {
                ModelState.AddModelError("", "Cette application ne peut pas être supprimée car elle est utilisée.");
                return View(app);
            }

            _db.DefApp.Remove(app);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted DefApp {AppId}", id);
            if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefApp", action = "Delete", id });
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> DefAppExists(int id)
        {
            return await _db.DefApp.AnyAsync(e => e.AppId == id);
        }
    }
}

