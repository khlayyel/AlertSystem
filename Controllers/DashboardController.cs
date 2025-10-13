using System.Security.Claims;
using AlertSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Controllers
{
    // Auth temporairement désactivée pendant la refonte
    public sealed class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}

