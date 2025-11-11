using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using AlertSystem.Data;
using Microsoft.AspNetCore.SignalR;
using AlertSystem.Infrastructure.Hubs;

namespace AlertSystem.WEB.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class DefAlerteController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DefAlerteController> _logger;
        private readonly IHubContext<NotificationHub>? _hub;

        public DefAlerteController(ApplicationDbContext db, ILogger<DefAlerteController> logger, IHubContext<NotificationHub>? hub = null)
        {
            _db = db;
            _logger = logger;
            _hub = hub;
        }

        // GET: DefAlerte
        public async Task<IActionResult> Index()
        {
            var defAlertes = await _db.DefAlerte
                .Include(d => d.TypeAlerte)
                .ThenInclude(t => t!.App)
                .OrderBy(d => d.DefAlerteId)
                .ToListAsync();

            // Parse ListDestinatairesId and get user emails
            var viewModel = defAlertes.Select(da =>
            {
                var destinataireIds = new List<int>();
                try
                {
                    destinataireIds = JsonSerializer.Deserialize<List<int>>(da.ListDestinatairesId) ?? new List<int>();
                }
                catch { }

                var emails = _db.DefUtilisateur
                    .Where(u => destinataireIds.Contains(u.UtilisateurId))
                    .Select(u => u.Email)
                    .ToList();

                return new
                {
                    DefAlerte = da,
                    DestinatairesEmails = string.Join(", ", emails)
                };
            }).ToList();

            return View(viewModel);
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(string? q = null, int page = 1, int pageSize = 10)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var qry = _db.DefAlerte.Include(d => d.TypeAlerte)!.ThenInclude(t => t!.App).AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var ql = q.Trim().ToLower();
                qry = qry.Where(d => (d.URL ?? "").ToLower().Contains(ql) || (d.TypeAlerte!.Description ?? "").ToLower().Contains(ql) || (d.TypeAlerte!.App!.Description ?? "").ToLower().Contains(ql));
            }
            var total = await qry.CountAsync();
            var items = await qry
                .OrderBy(d => d.DefAlerteId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new { d.DefAlerteId, d.URL, d.IsActive, type = d.TypeAlerte!.Description, app = d.TypeAlerte!.App!.Description })
                .ToListAsync();
            return Json(new { total, items, page, pageSize });
        }

        // GET: DefAlerte/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.TypeAlertes = new SelectList(
                await _db.DefTypeAlerte
                    .Include(t => t.App)
                    .OrderBy(t => t.App!.Description)
                    .ThenBy(t => t.Description)
                    .ToListAsync(),
                "TypeAlertId",
                "Description",
                null,
                "App.Description");

            ViewBag.Utilisateurs = await _db.DefUtilisateur
                .OrderBy(u => u.Username)
                .Select(u => new { u.UtilisateurId, u.Username, u.Email })
                .ToListAsync();

            return View();
        }

        // POST: DefAlerte/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AlertSystem.Entities.Entities.DefAlerte defAlerte, int[] selectedDestinataires)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Convert selected destinataires to JSON array
                    defAlerte.ListDestinatairesId = JsonSerializer.Serialize(selectedDestinataires ?? Array.Empty<int>());

                    // Validate URL
                    if (!Uri.TryCreate(defAlerte.URL, UriKind.Absolute, out _))
                    {
                        ModelState.AddModelError("URL", "L'URL doit être valide.");
                        ViewBag.TypeAlertes = new SelectList(
                            await _db.DefTypeAlerte.Include(t => t.App).OrderBy(t => t.App!.Description).ThenBy(t => t.Description).ToListAsync(),
                            "TypeAlertId",
                            "Description",
                            defAlerte.DefTypeAlerte,
                            "App.Description");
                        ViewBag.Utilisateurs = await _db.DefUtilisateur.OrderBy(u => u.Username).Select(u => new { u.UtilisateurId, u.Username, u.Email }).ToListAsync();
                        return View(defAlerte);
                    }

                    _db.DefAlerte.Add(defAlerte);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Created DefAlerte {DefAlerteId} with URL {URL}", defAlerte.DefAlerteId, defAlerte.URL);
                    if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefAlerte", action = "Create", id = defAlerte.DefAlerteId });
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating DefAlerte");
                    ModelState.AddModelError("", "Une erreur est survenue lors de la création.");
                }
            }

            ViewBag.TypeAlertes = new SelectList(
                await _db.DefTypeAlerte.Include(t => t.App).OrderBy(t => t.App!.Description).ThenBy(t => t.Description).ToListAsync(),
                "TypeAlertId",
                "Description",
                defAlerte.DefTypeAlerte,
                "App.Description");
            ViewBag.Utilisateurs = await _db.DefUtilisateur.OrderBy(u => u.Username).Select(u => new { u.UtilisateurId, u.Username, u.Email }).ToListAsync();
            return View(defAlerte);
        }

        // GET: DefAlerte/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var defAlerte = await _db.DefAlerte.FindAsync(id);
            if (defAlerte == null)
            {
                return NotFound();
            }

            ViewBag.TypeAlertes = new SelectList(
                await _db.DefTypeAlerte.Include(t => t.App).OrderBy(t => t.App!.Description).ThenBy(t => t.Description).ToListAsync(),
                "TypeAlertId",
                "Description",
                defAlerte.DefTypeAlerte,
                "App.Description");

            // Parse ListDestinatairesId to get selected user IDs
            var selectedIds = new List<int>();
            try
            {
                selectedIds = JsonSerializer.Deserialize<List<int>>(defAlerte.ListDestinatairesId) ?? new List<int>();
            }
            catch { }

            ViewBag.SelectedDestinataires = selectedIds;
            ViewBag.Utilisateurs = await _db.DefUtilisateur.OrderBy(u => u.Username).Select(u => new { u.UtilisateurId, u.Username, u.Email }).ToListAsync();

            return View(defAlerte);
        }

        // POST: DefAlerte/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AlertSystem.Entities.Entities.DefAlerte defAlerte, int[] selectedDestinataires)
        {
            if (id != defAlerte.DefAlerteId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Convert selected destinataires to JSON array
                    defAlerte.ListDestinatairesId = JsonSerializer.Serialize(selectedDestinataires ?? Array.Empty<int>());

                    // Validate URL
                    if (!Uri.TryCreate(defAlerte.URL, UriKind.Absolute, out _))
                    {
                        ModelState.AddModelError("URL", "L'URL doit être valide.");
                        ViewBag.TypeAlertes = new SelectList(
                            await _db.DefTypeAlerte.Include(t => t.App).OrderBy(t => t.App!.Description).ThenBy(t => t.Description).ToListAsync(),
                            "TypeAlertId",
                            "Description",
                            defAlerte.DefTypeAlerte,
                            "App.Description");
                        ViewBag.SelectedDestinataires = selectedDestinataires?.ToList() ?? new List<int>();
                        ViewBag.Utilisateurs = await _db.DefUtilisateur.OrderBy(u => u.Username).Select(u => new { u.UtilisateurId, u.Username, u.Email }).ToListAsync();
                        return View(defAlerte);
                    }

                    _db.Update(defAlerte);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Updated DefAlerte {DefAlerteId}", defAlerte.DefAlerteId);
                    if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefAlerte", action = "Edit", id = defAlerte.DefAlerteId });
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await DefAlerteExists(defAlerte.DefAlerteId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating DefAlerte {DefAlerteId}", defAlerte.DefAlerteId);
                    ModelState.AddModelError("", "Une erreur est survenue lors de la mise à jour.");
                }
            }

            ViewBag.TypeAlertes = new SelectList(
                await _db.DefTypeAlerte.Include(t => t.App).OrderBy(t => t.App!.Description).ThenBy(t => t.Description).ToListAsync(),
                "TypeAlertId",
                "Description",
                defAlerte.DefTypeAlerte,
                "App.Description");
            ViewBag.SelectedDestinataires = selectedDestinataires?.ToList() ?? new List<int>();
            ViewBag.Utilisateurs = await _db.DefUtilisateur.OrderBy(u => u.Username).Select(u => new { u.UtilisateurId, u.Username, u.Email }).ToListAsync();
            return View(defAlerte);
        }

        // GET: DefAlerte/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var defAlerte = await _db.DefAlerte
                .Include(d => d.TypeAlerte)
                .ThenInclude(t => t!.App)
                .FirstOrDefaultAsync(d => d.DefAlerteId == id);
            if (defAlerte == null)
            {
                return NotFound();
            }

            // Parse destinataires for display
            var destinataireIds = new List<int>();
            try
            {
                destinataireIds = JsonSerializer.Deserialize<List<int>>(defAlerte.ListDestinatairesId) ?? new List<int>();
            }
            catch { }

            var emails = await _db.DefUtilisateur
                .Where(u => destinataireIds.Contains(u.UtilisateurId))
                .Select(u => u.Email)
                .ToListAsync();

            ViewBag.DestinatairesEmails = string.Join(", ", emails);

            return View(defAlerte);
        }

        // POST: DefAlerte/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var defAlerte = await _db.DefAlerte.FindAsync(id);
            if (defAlerte == null)
            {
                return NotFound();
            }

            _db.DefAlerte.Remove(defAlerte);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted DefAlerte {DefAlerteId}", id);
            if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefAlerte", action = "Delete", id });
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> DefAlerteExists(int id)
        {
            return await _db.DefAlerte.AnyAsync(e => e.DefAlerteId == id);
        }
    }
}

