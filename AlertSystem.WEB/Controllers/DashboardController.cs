using System.Security.Claims;
using AlertSystem.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.WEB.Controllers
{
    // Auth temporairement désactivée pendant la refonte
    public sealed class DashboardController : Controller
    {
        private readonly IAlertReadService _read;
        public DashboardController(IAlertReadService read) { _read = read; }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}

