using System.Threading;
using System.Threading.Tasks;

namespace AlertSystem.Worker.Services
{
    public interface IWebPushNotifier
    {
        Task SendAsync(int userId, string title, string message, CancellationToken cancellationToken = default);
    }
}
