using System.Threading.Tasks;

namespace AlertSystem.Service.Interfaces
{
    public interface IApiKeyValidator
    {
        Task<bool> ValidateAsync(string apiKey);
    }
}

