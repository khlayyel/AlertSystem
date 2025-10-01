using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Services;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Controllers
{
    [Authorize(Roles="Admin")] // restreindre le test aux admins
    public sealed class EmailController : Controller
    {
        private readonly SmtpEmailSender _email;
        public EmailController(IConfiguration cfg, ILogger<SmtpEmailSender> logger){ _email = new SmtpEmailSender(cfg, logger); }

        [HttpPost]
        public async Task<IActionResult> Test([FromForm] string to)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(to)) return BadRequest("missing to");
                await _email.SendAsync(to, "[AlertSystem] Test email", "Ceci est un email de test.");
                return Json(new { ok = true });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }
    }
}


