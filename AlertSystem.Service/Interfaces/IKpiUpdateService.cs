namespace AlertSystem.Service
{
    public interface IKpiUpdateService
    {
        Task SendInboxKpiUpdateAsync(int userId);
        Task SendOutboxKpiUpdateAsync(int userId);
        Task SendAllKpiUpdateAsync(int userId);
        Task SendOutboxModalUpdateAsync(int userId, int alerteId, object recipientData);
        Task SendTestOutboxKpiUpdateAsync(int userId, object testData);
        Task SendNewAlertToOutboxAsync(int userId, object alertData);
        Task SendNewAlertToInboxAsync(int userId, object alertData);
    }
}
