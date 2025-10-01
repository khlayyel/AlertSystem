using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AlertSystem.Services
{
    public sealed class CurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly IHttpContextAccessor _http;

        public CurrentUserAccessor(IHttpContextAccessor http)
        {
            _http = http;
        }

        public int? GetUserId()
        {
            var id = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(id, out var uid)) return uid;
            return null; // fallback désactivé: on forcera les contrôleurs à gérer l'anonyme
        }

        public string GetRole()
        {
            return _http.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        public int? GetDepartmentId()
        {
            var dep = _http.HttpContext?.User?.FindFirst("department")?.Value;
            if (int.TryParse(dep, out var id)) return id;
            return null;
        }

        public bool IsAdmin() => string.Equals(GetRole(), "Admin", StringComparison.OrdinalIgnoreCase);
        public bool IsSuperUser() => string.Equals(GetRole(), "SuperUser", StringComparison.OrdinalIgnoreCase);
    }
}

