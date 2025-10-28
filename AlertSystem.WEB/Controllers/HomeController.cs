using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlertSystem.WEB.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // Check if user is authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                // User is logged in, redirect to dashboard
                return RedirectToAction("Inbox", "Dashboard");
            }
            else
            {
                // User is not logged in, redirect to login
                return RedirectToAction("Login", "Account");
            }
        }
    }
}