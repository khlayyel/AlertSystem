using Microsoft.AspNetCore.Mvc;
using AlertSystem.Models;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace AlertSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", new { area = "" });
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Plain-text check provisoire (en attendant authentification complète)
            var user = await _db.Users
                .SingleOrDefaultAsync(u => u.Email == model.Email);

            if (user != null)
            {
                bool ok = false;
                var stored = user.PasswordHash ?? string.Empty;
                // Support legacy: si pas Bcrypt (ne commence pas par "$2"), comparer en clair
                if (!string.IsNullOrWhiteSpace(stored) && stored.StartsWith("$2"))
                {
                    try { ok = BCrypt.Net.BCrypt.Verify(model.Password, stored); } catch { ok = false; }
                }
                else
                {
                    ok = string.Equals(stored, model.Password);
                    // Si OK en clair, migrer vers Bcrypt immédiatement
                    if (ok)
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                        await _db.SaveChangesAsync();
                    }
                }
                if (!ok) user = null;
            }

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                };
                if (user.DepartmentId.HasValue)
                {
                    claims.Add(new Claim("department", user.DepartmentId.Value.ToString()));
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties { IsPersistent = model.RememberMe });

                return Redirect(returnUrl ?? Url.Action("Index", "Dashboard")!);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }
    }
}

