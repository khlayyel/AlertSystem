using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AlertSystem.Worker.Services
{
    public interface IUserRecipientService
    {
        Task<List<(int PlatformId, string Recipient)>> GetRecipientsForUserAsync(int utilisateurId, CancellationToken ct);
    }
}

