using System.Security.Claims;
using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Entities.Entities;

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
        private readonly IHotelUserRepository _userRepository;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IHotelUserRepository userRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
        }

        public int? GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
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
                return await _userRepository.GetUserByIdAsync(userId.Value);
            }
            return null;
        }
    }
}
