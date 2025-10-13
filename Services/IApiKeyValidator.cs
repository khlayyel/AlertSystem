using System.Threading.Tasks;
using AlertSystem.Models.Entities;

namespace AlertSystem.Services
{
    public interface IApiKeyValidator
    {
        Task<bool> IsValidAsync(string? apiKey);
        Task<bool> ValidateApiKeyAsync(string? apiKey);
        Task<ApiClient?> GetClientByApiKeyAsync(string? apiKey);
    }
}


