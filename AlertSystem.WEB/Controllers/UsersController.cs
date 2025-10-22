using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;

namespace AlertSystem.WEB.Controllers
{
    [Route("Users")]
    public sealed class UsersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public UsersController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("List")]
        public async Task<IActionResult> List()
        {
            try
            {
                var users = await _db.Users
                    .Select(u => new
                    {
                        userId = u.UserId,
                        fullName = u.FullName,
                        email = u.Email,
                        phoneNumber = u.PhoneNumber
                    })
                    .ToListAsync();

                return Json(new { users });
            }
            catch (Exception ex)
            {
                return Json(new { users = new object[0], error = ex.Message });
            }
        }
    }
}


