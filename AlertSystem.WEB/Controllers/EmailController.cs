using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Service.Services;
using AlertSystem.Service.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AlertSystem.Utils.Email;

namespace AlertSystem.WEB.Controllers
{
    [Authorize]
    public sealed class EmailController : Controller
    {
        private readonly AlertSystem.Service.Services.NotificationService _email;
        private readonly IConfiguration _config;
        
        public EmailController(AlertSystem.Service.Services.NotificationService email, IConfiguration config)
        { 
            _email = email; 
            _config = config;
        }

        [HttpGet]
        public IActionResult TestConfig()
        {
            try
            {
                var config = SmtpConfigurationUtils.GetSmtpConfiguration(_config);
                return Json(new { 
                    ok = true, 
                    config = new {
                        host = config.Host,
                        port = config.Port,
                        user = config.User,
                        password = string.IsNullOrEmpty(config.Password) ? "EMPTY" : "SET",
                        useStartTls = config.UseStartTls,
                        useSsl = config.UseSsl,
                        fromName = config.FromName,
                        fromEmail = config.FromEmail
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

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


