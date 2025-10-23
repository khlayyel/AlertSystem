using System.Threading.Tasks;

namespace AlertSystem.Service
{
    public interface IAlertReadService
    {
        Task<int> GetUnreadCountAsync();
        Task<int> GetUnreadCountAsync(int userId);
        Task<int> GetTodayCountAsync();
        Task<int> GetTodayCountAsync(int userId);
        Task<int> GetConfirmedMandatoryCountAsync();
        Task<int> GetConfirmedMandatoryCountAsync(int userId);
        Task<int> GetMandatoryPendingCountAsync();
        Task<int> GetMandatoryPendingCountAsync(int userId);
        Task<int> GetSentTodayCountAsync(int userId);
        Task<int> GetFailedCountAsync(int userId);
        Task<object> GetInboxAsync(int page, int size, string status = "all");
        Task<object> GetHistoryAsync(string status, int page, int size);
        Task<object> GetSentAsync(int page, int size);
        Task<object?> GetDetailsAsync(int id);
        Task<bool> MarkAsReadAsync(int alertRecipientId);
    }
}


