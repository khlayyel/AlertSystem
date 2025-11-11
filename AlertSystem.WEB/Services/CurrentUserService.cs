using System.Security.Claims;
using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.WEB.Services
{
    public interface ICurrentUserService
    {
        Task<DefUtilisateur?> GetCurrentUserAsync();
        int? GetCurrentUserId();
        string? GetCurrentUserEmail();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _db;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext db)
        {
            _httpContextAccessor = httpContextAccessor;
            _db = db;
        }

        public int? GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        public string? GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
        }

        public async Task<DefUtilisateur?> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                return await _db.DefUtilisateur.AsNoTracking().FirstOrDefaultAsync(u => u.UtilisateurId == userId.Value);
            }
            var email = GetCurrentUserEmail();
            if (!string.IsNullOrWhiteSpace(email))
            {
                return await _db.DefUtilisateur.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            }
            return null;
        }
    }
}
