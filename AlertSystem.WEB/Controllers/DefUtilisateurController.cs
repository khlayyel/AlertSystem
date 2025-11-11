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
    public class DefUtilisateurController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DefUtilisateurController> _logger;
        private readonly IHubContext<NotificationHub>? _hub;

        public DefUtilisateurController(ApplicationDbContext db, ILogger<DefUtilisateurController> logger, IHubContext<NotificationHub>? hub = null)
        {
            _db = db;
            _logger = logger;
            _hub = hub;
        }

        // GET: DefUtilisateur
        public async Task<IActionResult> Index()
        {
            var utilisateurs = await _db.DefUtilisateur
                .Include(u => u.App)
                .OrderBy(u => u.Username)
                .ToListAsync();
            return View(utilisateurs);
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(string? q = null, int page = 1, int pageSize = 10)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var qry = _db.DefUtilisateur.Include(u => u.App).AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var ql = q.Trim().ToLower();
                qry = qry.Where(u => (u.Username ?? "").ToLower().Contains(ql) || (u.Email ?? "").ToLower().Contains(ql) || (u.App!.Description ?? "").ToLower().Contains(ql));
            }
            var total = await qry.CountAsync();
            var items = await qry
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new { u.UtilisateurId, u.Username, u.Email, u.WhatsAppNumber, u.AppId, app = u.App!.Description })
                .ToListAsync();
            return Json(new { total, items, page, pageSize });
        }

        private async Task PopulateAppsAsync(int? selectedAppId = null)
        {
            ViewBag.Apps = new SelectList(
                await _db.DefApp.OrderBy(a => a.Description).ToListAsync(),
                "AppId",
                "Description",
                selectedAppId);
        }

        // GET: DefUtilisateur/Create
        public async Task<IActionResult> Create()
        {
            await PopulateAppsAsync();
            return View();
        }

        // POST: DefUtilisateur/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AlertSystem.Entities.Entities.DefUtilisateur utilisateur)
        {
            if (string.IsNullOrWhiteSpace(utilisateur.Password))
            {
                ModelState.AddModelError("Password", "Le mot de passe est obligatoire.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateAppsAsync(utilisateur.AppId);
                return View(utilisateur);
            }

            // Simple validation for email uniqueness
            var exists = await _db.DefUtilisateur.AnyAsync(u => u.Email == utilisateur.Email);
            if (exists)
            {
                ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                await PopulateAppsAsync(utilisateur.AppId);
                return View(utilisateur);
            }

            // Normalize WhatsApp number (optional +216 prefix)
            if (!string.IsNullOrWhiteSpace(utilisateur.WhatsAppNumber))
            {
                var phone = utilisateur.WhatsAppNumber.Trim();
                if (!phone.StartsWith("+"))
                {
                    phone = "+216" + phone.TrimStart('0');
                }
                utilisateur.WhatsAppNumber = phone;
            }

            try
            {
                utilisateur.Password = utilisateur.Password.Trim();
                _db.DefUtilisateur.Add(utilisateur);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Created DefUtilisateur {UserId} ({Email})", utilisateur.UtilisateurId, utilisateur.Email);
                if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefUtilisateur", action = "Create", id = utilisateur.UtilisateurId });
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating DefUtilisateur");
                ModelState.AddModelError("", "Une erreur est survenue lors de la création.");
                await PopulateAppsAsync(utilisateur.AppId);
                return View(utilisateur);
            }
        }

        // GET: DefUtilisateur/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var utilisateur = await _db.DefUtilisateur.FindAsync(id);
            if (utilisateur == null)
            {
                return NotFound();
            }

            await PopulateAppsAsync(utilisateur.AppId);
            return View(utilisateur);
        }

        // POST: DefUtilisateur/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AlertSystem.Entities.Entities.DefUtilisateur utilisateur)
        {
            if (id != utilisateur.UtilisateurId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateAppsAsync(utilisateur.AppId);
                return View(utilisateur);
            }

            var entity = await _db.DefUtilisateur.AsNoTracking().FirstOrDefaultAsync(u => u.UtilisateurId == id);
            if (entity == null)
            {
                return NotFound();
            }

            // Email uniqueness (excluding current user)
            var exists = await _db.DefUtilisateur
                .AnyAsync(u => u.Email == utilisateur.Email && u.UtilisateurId != utilisateur.UtilisateurId);
            if (exists)
            {
                ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                await PopulateAppsAsync(utilisateur.AppId);
                return View(utilisateur);
            }

            // Normalize WhatsApp number
            string? normalizedWhatsApp = null;
            if (!string.IsNullOrWhiteSpace(utilisateur.WhatsAppNumber))
            {
                var phone = utilisateur.WhatsAppNumber.Trim();
                if (!phone.StartsWith("+"))
                {
                    phone = "+216" + phone.TrimStart('0');
                }
                normalizedWhatsApp = phone;
            }

            var newPassword = string.IsNullOrWhiteSpace(utilisateur.Password) ? entity.Password : utilisateur.Password.Trim();

            try
            {
                entity.Username = utilisateur.Username;
                entity.Email = utilisateur.Email;
                entity.Password = newPassword;
                entity.AppId = utilisateur.AppId;
                entity.WhatsAppNumber = normalizedWhatsApp;

                _db.DefUtilisateur.Update(entity);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Updated DefUtilisateur {UserId}", entity.UtilisateurId);
                if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefUtilisateur", action = "Edit", id = entity.UtilisateurId });
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await DefUtilisateurExists(utilisateur.UtilisateurId))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating DefUtilisateur {UserId}", utilisateur.UtilisateurId);
                ModelState.AddModelError("", "Une erreur est survenue lors de la mise à jour.");
                await PopulateAppsAsync(utilisateur.AppId);
                return View(utilisateur);
            }
        }

        // GET: DefUtilisateur/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var utilisateur = await _db.DefUtilisateur
                .Include(u => u.App)
                .FirstOrDefaultAsync(u => u.UtilisateurId == id);
            if (utilisateur == null)
            {
                return NotFound();
            }

            var destinatairesJson = await _db.DefAlerte.Select(a => a.ListDestinatairesId).ToListAsync();
            var usedInAlertes = destinatairesJson.Any(json =>
            {
                try
                {
                    var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json ?? "[]") ?? new List<int>();
                    return ids.Contains(utilisateur.UtilisateurId);
                }
                catch
                {
                    return false;
                }
            });

            if (usedInAlertes)
            {
                ViewBag.ErrorMessage = "Cet utilisateur ne peut pas être supprimé car il est utilisé dans des alertes définies.";
            }

            return View(utilisateur);
        }

        // POST: DefUtilisateur/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var utilisateur = await _db.DefUtilisateur.FindAsync(id);
            if (utilisateur == null)
            {
                return NotFound();
            }

            var destinatairesJson = await _db.DefAlerte.Select(a => a.ListDestinatairesId).ToListAsync();
            var usedInAlertes = destinatairesJson.Any(json =>
            {
                try
                {
                    var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json ?? "[]") ?? new List<int>();
                    return ids.Contains(utilisateur.UtilisateurId);
                }
                catch
                {
                    return false;
                }
            });

            if (usedInAlertes)
            {
                ModelState.AddModelError("", "Cet utilisateur ne peut pas être supprimé car il est utilisé dans des alertes définies.");
                utilisateur = await _db.DefUtilisateur.Include(u => u.App).FirstOrDefaultAsync(u => u.UtilisateurId == id);
                return View(utilisateur ?? new AlertSystem.Entities.Entities.DefUtilisateur());
            }

            _db.DefUtilisateur.Remove(utilisateur);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted DefUtilisateur {UserId}", id);
            if (_hub != null) await _hub.Clients.All.SendAsync("AdminConfigUpdated", new { entity = "DefUtilisateur", action = "Delete", id });
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> DefUtilisateurExists(int id)
        {
            return await _db.DefUtilisateur.AnyAsync(e => e.UtilisateurId == id);
        }
    }
}
