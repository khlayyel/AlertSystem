using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Utils.Crypto;

namespace AlertSystem.Service
{
    public sealed class ApiKeyValidator : IApiKeyValidator
    {
        private readonly IApiClientRepository _apiClientRepository;

        public ApiKeyValidator(IApiClientRepository apiClientRepository)
        {
            _apiClientRepository = apiClientRepository;
        }

        public async Task<bool> ValidateAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return false;

            var normalized = apiKey.Trim();
            var hash = CryptoUtils.ComputeSha256(normalized);

            var client = await _apiClientRepository.GetByApiKeyHashAsync(hash);
            return client?.IsActive == true;
        }
    }
}


