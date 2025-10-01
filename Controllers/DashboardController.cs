using System.Security.Claims;
using AlertSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Controllers
{
    [Authorize]
    public sealed class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var name = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            ViewBag.DisplayName = name;
            ViewBag.Role = role;
            ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            return View();
        }
    }
}

