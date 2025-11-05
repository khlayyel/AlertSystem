using System.Threading;
using System.Threading.Tasks;

namespace AlertSystem.Worker.Services
{
    public interface IHotelApiClient
    {
        Task<string> GetAsync(string url, CancellationToken ct);
    }
}


