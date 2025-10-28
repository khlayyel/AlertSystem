using AlertSystem.Service.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using AlertSystem.Entities.Entities;

namespace AlertSystem.Service
{
    public interface IAlertReadService
    {
        Task<int> GetUnreadCountAsync();
        Task<int> GetTodayCountAsync();
        Task<int> GetConfirmedMandatoryCountAsync();
        Task<int> GetUnconfirmedMandatoryCountAsync();
        Task<int> GetCountByStatusAsync(int statusId);
        Task<int> GetCountByTypeAsync(int alertTypeId);
        Task<int> GetCountByPlatformAsync(int platformId);
        Task<IEnumerable<Alerte>> GetUnreadAlertsAsync(int page = 1, int pageSize = 20);
        Task<IEnumerable<Alerte>> GetAlertsByGroupAsync(Guid groupId);
        Task<IEnumerable<Alerte>> GetAlertsByRecipientAsync(string recipient, int platformId);
        Task<IEnumerable<Alerte>> GetAlertsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Alerte>> GetPendingAlertsAsync();
        Task<IEnumerable<Alerte>> GetAlertsByUserAsync(int userId);
        Task<bool> MarkAsReadAsync(int alertRecordId);
        Task<bool> MarkGroupAsReadAsync(Guid groupId);
        Task<AlertStatistics> GetStatisticsAsync();
    }
}


