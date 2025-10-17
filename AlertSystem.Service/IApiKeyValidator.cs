using System.Threading.Tasks;

namespace AlertSystem.Service
{
    public interface IApiKeyValidator
    {
        Task<bool> ValidateAsync(string apiKey);
    }
}

