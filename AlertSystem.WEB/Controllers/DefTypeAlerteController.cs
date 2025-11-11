using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;
using Microsoft.AspNetCore.SignalR;
using AlertSystem.Infrastructure.Hubs;

namespace AlertSystem.WEB.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class DefTypeAlerteController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DefTypeAlerteController> _logger;
        private readonly IHubContext<NotificationHub>? _hub;

        public DefTypeAlerteController(ApplicationDbContext db, ILogger<DefTypeAlerteController> logger, IHubContext<NotificationHub>? hub = null)
        {
            _db = db;
            _logger = logger;
            _hub = hub;
        }

        // GET: DefTypeAlerte
        public async Task<IActionResult> Index()
        {
            var typeAlertes = await _db.DefTypeAlerte
                .Include(t => t.App)
                .OrderBy(t => t.AppId)
                .ThenBy(t => t.TypeAlertId)
                .ToListAsync();
            return View(typeAlertes);
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(string? q = null, int page = 1, int pageSize = 10)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var qry = _db.DefTypeAlerte.Include(t => t.App).AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var ql = q.Trim().ToLower();
                qry = qry.Where(t => (t.Description ?? "").ToLower().Contains(ql) || (t.App!.Description ?? "").ToLower().Contains(ql));
            }
            var total = await qry.CountAsync();
            var items = await qry
                .OrderBy(t => t.AppId).ThenBy(t => t.TypeAlertId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new { t.TypeAlertId, t.Description, t.AppId, app = t.App!.Description })
                .ToListAsync();
            return Json(new { total, items, page, pageSize });
        }

        // GET: DefTypeAlerte/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Apps = new SelectList(await _db.DefApp.OrderBy(a => a.Description).ToListAsync(), "AppId", "Description");
            return View();
        }

        // POST: DefTypeAlerte/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AlertSystem.Entities.Entities.DefTypeAlerte typeAlerte)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _db.DefTypeAlerte.Add(typeAlerte);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Created DefTypeAlerte {TypeAlertId}: {Description}", typeAlerte.TypeAlertId, typeAlerte.Description);
                    if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefTypeAlerte", action = "Create", id = typeAlerte.TypeAlertId });
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating DefTypeAlerte");
                    ModelState.AddModelError("", "Une erreur est survenue lors de la création.");
                }
            }
            ViewBag.Apps = new SelectList(await _db.DefApp.OrderBy(a => a.Description).ToListAsync(), "AppId", "Description", typeAlerte.AppId);
            return View(typeAlerte);
        }

        // GET: DefTypeAlerte/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var typeAlerte = await _db.DefTypeAlerte.FindAsync(id);
            if (typeAlerte == null)
            {
                return NotFound();
            }

            ViewBag.Apps = new SelectList(await _db.DefApp.OrderBy(a => a.Description).ToListAsync(), "AppId", "Description", typeAlerte.AppId);
            return View(typeAlerte);
        }

        // POST: DefTypeAlerte/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AlertSystem.Entities.Entities.DefTypeAlerte typeAlerte)
        {
            if (id != typeAlerte.TypeAlertId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _db.Update(typeAlerte);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Updated DefTypeAlerte {TypeAlertId}: {Description}", typeAlerte.TypeAlertId, typeAlerte.Description);
                    if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefTypeAlerte", action = "Edit", id = typeAlerte.TypeAlertId });
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await DefTypeAlerteExists(typeAlerte.TypeAlertId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating DefTypeAlerte {TypeAlertId}", typeAlerte.TypeAlertId);
                    ModelState.AddModelError("", "Une erreur est survenue lors de la mise à jour.");
                }
            }
            ViewBag.Apps = new SelectList(await _db.DefApp.OrderBy(a => a.Description).ToListAsync(), "AppId", "Description", typeAlerte.AppId);
            return View(typeAlerte);
        }

        // GET: DefTypeAlerte/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var typeAlerte = await _db.DefTypeAlerte
                .Include(t => t.App)
                .FirstOrDefaultAsync(t => t.TypeAlertId == id);
            if (typeAlerte == null)
            {
                return NotFound();
            }

            // Check if type is used by any def_alerte
            var usedInDefAlertes = await _db.DefAlerte.AnyAsync(a => a.DefTypeAlerte == id);
            if (usedInDefAlertes)
            {
                ViewBag.ErrorMessage = "Ce type d'alerte ne peut pas être supprimé car il est utilisé par des alertes définies.";
            }

            return View(typeAlerte);
        }

        // POST: DefTypeAlerte/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var typeAlerte = await _db.DefTypeAlerte.FindAsync(id);
            if (typeAlerte == null)
            {
                return NotFound();
            }

            // Check if type is used
            var usedInDefAlertes = await _db.DefAlerte.AnyAsync(a => a.DefTypeAlerte == id);
            if (usedInDefAlertes)
            {
                ModelState.AddModelError("", "Ce type d'alerte ne peut pas être supprimé car il est utilisé.");
                typeAlerte = await _db.DefTypeAlerte.Include(t => t.App).FirstOrDefaultAsync(t => t.TypeAlertId == id);
                return View(typeAlerte);
            }

            _db.DefTypeAlerte.Remove(typeAlerte);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted DefTypeAlerte {TypeAlertId}", id);
            if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefTypeAlerte", action = "Delete", id });
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> DefTypeAlerteExists(int id)
        {
            return await _db.DefTypeAlerte.AnyAsync(e => e.TypeAlertId == id);
        }
    }
}

