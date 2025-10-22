using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlertSystem.Worker.Models;

namespace AlertSystem.Worker.Services
{
    public interface IAlertRepository
    {
        Task<List<AlerteModel>> GetUnprocessedAlertsAsync(CancellationToken cancellationToken = default);
        Task<List<UserModel>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
        Task<UserModel?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
        Task MarkAlertAsProcessedAsync(int alerteId, CancellationToken cancellationToken = default);
        Task MarkAlertAsFailedAsync(int alerteId, CancellationToken cancellationToken = default);
        Task CreateHistoriqueAlerteAsync(int alerteId, int userId, string email, string phoneNumber, string desktopToken, CancellationToken cancellationToken = default);
        Task<List<AlerteModel>> GetReminderAlertsAsync(CancellationToken cancellationToken = default);
        Task SetInitialReminderAsync(int alerteId, int intervalMinutes, CancellationToken cancellationToken = default);
        Task<bool> UpdateReminderStatusAsync(int alerteId, bool success, int intervalMinutes, int maxAttempts, CancellationToken cancellationToken = default);
        Task InsertReminderHistoryAsync(int alerteId, int historiqueAlerteId, bool success, int attemptNumber, string? errorDetails, CancellationToken cancellationToken = default);
        Task<List<int>> GetUnconfirmedRecipientsAsync(int alerteId, CancellationToken cancellationToken = default);
    }
}
