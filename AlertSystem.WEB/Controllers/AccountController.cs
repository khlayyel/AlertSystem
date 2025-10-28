using AlertSystem.Models;
using AlertSystem.DataLayer.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AlertSystem.WEB.Services;

namespace AlertSystem.WEB.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IHotelUserRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IHotelUserRepository userRepository, IPasswordService passwordService, ILogger<AccountController> logger)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
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

            // Get user from hotel database def_utilisateur table
            var user = await _userRepository.GetUserByEmailAsync(model.Email);
            
            if (user == null || !user.util_compte_active)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            // Verify password using 3DES (with fallbacks)
            var ok = _passwordService.VerifyPassword(model.Password, user.util_password);
            if (!ok)
            {
                try
                {
                    var decrypted = _passwordService.DecryptPassword(user.util_password);
                    _logger.LogWarning("Login failed for {Email}. Provided='{Provided}', StoredLen={Len}, Decrypted='{Decrypted}'",
                        model.Email, model.Password, user.util_password?.Length ?? 0, string.IsNullOrEmpty(decrypted) ? "<fail>" : decrypted);
                }
                catch { }
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }
            
            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.util_id.ToString()),
                new Claim(ClaimTypes.Name, user.util_nom),
                new Claim(ClaimTypes.Email, user.util_email ?? "")
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

            // Ensure user has a desktop token: create one if missing and store in DefUtilisateur if needed
            try
            {
                var deviceTokenCookie = Request.Cookies["as_desktop_token"];
                if (string.IsNullOrWhiteSpace(deviceTokenCookie))
                {
                    deviceTokenCookie = Guid.NewGuid().ToString("N");
                    Response.Cookies.Append("as_desktop_token", deviceTokenCookie, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        HttpOnly = false,
                        IsEssential = true
                    });
                }

                // Upsert into WebPushSubscriptions for this user
                if (int.TryParse(user.util_id.ToString(), out var uid))
                {
                    // Use PushController endpoints internally would require HTTP; we write direct via DbContext
                    // Minimal upsert: if token not exists for user, create a placeholder subscription row
                    // This is safe: real push subscription will overwrite endpoint/keys later
                    var db = HttpContext.RequestServices.GetRequiredService<AlertSystem.Data.ApplicationDbContext>();
                    var exists = db.WebPushSubscriptions
                        .Any(s => s.UserId == uid && s.Endpoint == deviceTokenCookie);
                    if (!exists)
                    {
                        db.WebPushSubscriptions.Add(new AlertSystem.Entities.Entities.WebPushSubscription
                        {
                            UserId = uid,
                            Endpoint = deviceTokenCookie,
                            P256dh = string.Empty,
                            Auth = string.Empty,
                            CreatedAt = DateTime.UtcNow
                        });
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch { }

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

