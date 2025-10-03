using AlertSystem.Data;
using AlertSystem.Models.Entities;
using AlertSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Services;
using Microsoft.AspNetCore.SignalR;
using AlertSystem.Hubs;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;

namespace AlertSystem.Controllers
{
    // NOTE: En attendant le login, pas de restriction d'accès ici.
    [Authorize(Roles = "Admin,SuperUser")]
    public sealed class UsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserAccessor _current;
        private readonly IHubContext<NotificationsHub> _hub;

        public UsersController(ApplicationDbContext db, ICurrentUserAccessor current, IHubContext<NotificationsHub> hub)
        {
            _db = db;
            _current = current;
            _hub = hub;
        }

        // GET: /Users
        [HttpGet]
        public async Task<IActionResult> Index(string? role, int? departmentId, string? q, int page = 1, int size = 10, string? sortBy = "Username", bool desc = false)
        {
            var departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            var query = _db.Users.AsNoTracking().AsQueryable();

            // Scoping strict par rôle
            if (_current.IsSuperUser() && _current.GetDepartmentId().HasValue)
            {
                int depId = _current.GetDepartmentId()!.Value;
                // SuperUser: uniquement les Users de SON département
                query = query.Where(u => u.DepartmentId == depId && u.Role == "User");
                departmentId = depId; // force le filtre département dans l'UI
                role = "User";        // force le filtre rôle dans l'UI
            }
            // Admin voit tous les utilisateurs de tous les départements (pas de restriction)
            if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role);
            if (departmentId.HasValue) query = query.Where(u => u.DepartmentId == departmentId.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(u => u.Username.Contains(q) || u.Email.Contains(q));
            }

            query = (sortBy, desc) switch
            {
                ("Email", false) => query.OrderBy(u => u.Email),
                ("Email", true)  => query.OrderByDescending(u => u.Email),
                ("Role", false)  => query.OrderBy(u => u.Role),
                ("Role", true)   => query.OrderByDescending(u => u.Role),
                _ when desc       => query.OrderByDescending(u => u.Username),
                _                 => query.OrderBy(u => u.Username)
            };

            int total = await query.CountAsync();
            var items = await query.Skip((page-1)*size).Take(size).ToListAsync();

            var vm = new UserListViewModel
            {
                Items = items,
                Departments = departments,
                Role = role,
                DepartmentId = departmentId,
                Query = q,
                Page = page,
                Size = size,
                TotalCount = total,
                SortBy = sortBy,
                Desc = desc
            };
            return View(vm);
        }

        // Recherche live JSON
        [HttpGet]
        public async Task<IActionResult> Search(string? role, int? departmentId, string? q, int page = 1, int size = 10, string? sortBy = "Username", bool desc = false)
        {
            var query = _db.Users.AsNoTracking().AsQueryable();

            if (_current.IsSuperUser() && _current.GetDepartmentId().HasValue)
            {
                int depId = _current.GetDepartmentId()!.Value;
                // SuperUser: uniquement Users de SON département
                query = query.Where(u => u.DepartmentId == depId && u.Role == "User");
                departmentId = depId; role = "User";
            }
            // Admin voit tous les utilisateurs (pas de restriction)

            if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role);
            if (departmentId.HasValue) query = query.Where(u => u.DepartmentId == departmentId.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(u => u.Username.Contains(q) || u.Email.Contains(q));
            }

            query = (sortBy, desc) switch
            {
                ("Email", false) => query.OrderBy(u => u.Email),
                ("Email", true)  => query.OrderByDescending(u => u.Email),
                ("Role", false)  => query.OrderBy(u => u.Role),
                ("Role", true)   => query.OrderByDescending(u => u.Role),
                _ when desc       => query.OrderByDescending(u => u.Username),
                _                 => query.OrderBy(u => u.Username)
            };

            int total = await query.CountAsync();
            var items = await query.Skip((page-1)*size).Take(size).Select(u => new {
                u.UserId, u.Username, u.Email, u.Role, u.DepartmentId
            }).ToListAsync();
            return Json(new { items, totalCount = total, page, size });
        }

        // GET: /Users/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            ViewBag.AllowedRoles = GetAllowedRoles();
            return View(new User());
        }

        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
                ViewBag.AllowedRoles = GetAllowedRoles();
                return View(model);
            }
            // Validation simple: email unique
            bool emailExists = await _db.Users.AnyAsync(u => u.Email == model.Email && u.UserId != model.UserId);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email déjà utilisé");
                ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
                ViewBag.AllowedRoles = GetAllowedRoles();
                return View(model);
            }

            // Restriction SuperUser: ne peut créer que dans son département
            if (_current.IsSuperUser() && _current.GetDepartmentId().HasValue)
            {
                model.DepartmentId = _current.GetDepartmentId();
                // SuperUser ne peut créer que des Users (pas Admin/SuperUser)
                model.Role = "User";
            }
            else
            {
                // Admin: ne peut créer que des rôles valides
                if (!GetAllowedRoles().Contains(model.Role)) model.Role = "User";
                // Si Admin est sélectionné, forcer DepartmentId à null
                if (model.Role == "Admin") model.DepartmentId = null;
            }

            // Hash mot de passe si non vide
            if (!string.IsNullOrWhiteSpace(model.PasswordHash))
            {
                model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
            }
            _db.Users.Add(model);
            await _db.SaveChangesAsync();
            
            // Notify all clients of user changes
            await _hub.Clients.All.SendAsync("usersChanged");
            await _hub.Clients.All.SendAsync("userCreated");
            
            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            ViewBag.AllowedRoles = GetAllowedRoles();
            // SuperUser ne peut pas éditer un utilisateur d'un autre département ni un Admin/SuperUser
            if (_current.IsSuperUser())
            {
                var depId = _current.GetDepartmentId();
                if (user.DepartmentId != depId) return Forbid();
                if (!string.Equals(user.Role, "User", StringComparison.OrdinalIgnoreCase)) return Forbid();
            }
            return View(user);
        }

        // POST: /Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model)
        {
            if (id != model.UserId) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
                ViewBag.AllowedRoles = GetAllowedRoles();
                return View(model);
            }
            bool emailExists = await _db.Users.AnyAsync(u => u.Email == model.Email && u.UserId != model.UserId);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email déjà utilisé");
                ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
                ViewBag.AllowedRoles = GetAllowedRoles();
                return View(model);
            }

            // Charger l'utilisateur actuel pour éviter d'écraser accidentellement son mot de passe
            var existing = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (existing == null) return NotFound();

            // Mise à jour des champs simples
            existing.Username = model.Username;
            existing.Email = model.Email;
            existing.PhoneNumber = model.PhoneNumber;

            // Règles de rôle/département
            if (_current.IsSuperUser() && _current.GetDepartmentId().HasValue)
            {
                existing.DepartmentId = _current.GetDepartmentId();
                existing.Role = "User"; // SuperUser garde le rôle User
            }
            else
            {
                existing.Role = GetAllowedRoles().Contains(model.Role) ? model.Role : "User";
                existing.DepartmentId = existing.Role == "Admin" ? null : model.DepartmentId;
            }

            // Mot de passe: ne modifier que si un nouveau a été fourni
            if (!string.IsNullOrWhiteSpace(model.PasswordHash))
            {
                existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
            }

            await _db.SaveChangesAsync();
            
            // Notify all clients of user changes
            await _hub.Clients.All.SendAsync("usersChanged");
            await _hub.Clients.All.SendAsync("userUpdated");
            
            return RedirectToAction(nameof(Index));
        }

        // POST: /Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (_current.IsSuperUser() && _current.GetDepartmentId().HasValue && user.DepartmentId != _current.GetDepartmentId())
            {
                return Forbid();
            }
            if (_current.IsSuperUser() && !string.Equals(user.Role, "User", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            
            // Notify all clients of user changes
            await _hub.Clients.All.SendAsync("usersChanged");
            await _hub.Clients.All.SendAsync("userDeleted");
            
            return RedirectToAction(nameof(Index));
        }

        private static IReadOnlyList<string> GetAllowedRolesForAdmin() => new List<string> { "Admin", "SuperUser", "User" };
        private IReadOnlyList<string> GetAllowedRoles() => _current.IsSuperUser() ? new List<string> { "User" } : GetAllowedRolesForAdmin();
    }
}

