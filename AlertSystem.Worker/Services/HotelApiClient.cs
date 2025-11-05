using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AlertSystem.Worker.Services
{
    public sealed class HotelApiClient : IHotelApiClient
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _configuration;
        public HotelApiClient(IHttpClientFactory factory, IConfiguration configuration){ _factory = factory; _configuration = configuration; }

        public async Task<string> GetAsync(string url, CancellationToken ct)
        {
            using var client = _factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var apiKey = _configuration["Polling:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.TryAddWithoutValidation("X-API-KEY", apiKey);
            }
            using var resp = await client.SendAsync(request, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(ct);
        }
    }
}


