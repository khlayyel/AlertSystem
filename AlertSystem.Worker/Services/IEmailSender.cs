using System.Threading;
using System.Threading.Tasks;

namespace AlertSystem.Worker.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string email, string fullName, string title, string message, CancellationToken cancellationToken = default);
    }
}
