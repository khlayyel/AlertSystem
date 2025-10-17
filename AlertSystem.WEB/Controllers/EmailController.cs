using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service;
using Microsoft.Extensions.Logging;

namespace AlertSystem.WEB.Controllers
{
    public sealed class EmailController : Controller
    {
        private readonly IEmailSender _email;
        public EmailController(IEmailSender email){ _email = email; }

        [HttpPost]
        public async Task<IActionResult> Test([FromForm] string to)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(to)) return BadRequest("missing to");
                await _email.SendEmailAsync(to, "[AlertSystem] Test email", "Ceci est un email de test.");
                return Json(new { ok = true });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }
    }
}


