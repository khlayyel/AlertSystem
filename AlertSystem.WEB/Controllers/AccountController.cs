using AlertSystem.Models;
using AlertSystem.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AlertSystem.WEB.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext db, ILogger<AccountController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get user from def_Utilisateur table
            var user = await _db.DefUtilisateur
                .Where(u => u.Email == model.Email)
                .FirstOrDefaultAsync();
            
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            // Verify password (simple comparison for now - in production, use BCrypt or similar)
            if (user.Password != model.Password)
            {
                _logger.LogWarning("Login failed for {Email}: invalid password", model.Email);
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }
            
            // Determine role: Admin for khalilouerghemmi@gmail.com and zied.soltani11@gmail.com
            var isAdmin = model.Email.Equals("khalilouerghemmi@gmail.com", StringComparison.OrdinalIgnoreCase) ||
                         model.Email.Equals("zied.soltani11@gmail.com", StringComparison.OrdinalIgnoreCase);
            var role = isAdmin ? "Admin" : "User";
            
            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UtilisateurId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role),
                new Claim("AppId", user.AppId.ToString())
            };

            // Create identity
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Sign in
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe
                });

            _logger.LogInformation("User {Email} logged in with role {Role}", model.Email, role);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
