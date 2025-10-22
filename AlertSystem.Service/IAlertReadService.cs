using System.Threading.Tasks;

namespace AlertSystem.Service
{
    public interface IAlertReadService
    {
        Task<int> GetUnreadCountAsync();
        Task<int> GetTodayCountAsync();
        Task<int> GetConfirmedMandatoryCountAsync();
        Task<int> GetMandatoryPendingCountAsync();
        Task<object> GetInboxAsync(int page, int size, string status = "all");
        Task<object> GetHistoryAsync(string status, int page, int size);
        Task<object> GetSentAsync(int page, int size);
        Task<object?> GetDetailsAsync(int id);
        Task<bool> MarkAsReadAsync(int alertRecipientId);
    }
}


