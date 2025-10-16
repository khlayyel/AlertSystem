using System.Threading;
using System.Threading.Tasks;

namespace AlertSystem.Worker.Services
{
    public interface IWhatsAppSender
    {
        Task SendAsync(string phoneNumber, string title, string message, CancellationToken cancellationToken = default);
    }
}
