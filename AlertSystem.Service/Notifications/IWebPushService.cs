namespace AlertSystem.Services
{
    public interface IWebPushService
    {
        Task<bool> SendNotificationAsync(string deviceToken, string title, string message);
        Task<string> SubscribeUserAsync(int userId, string endpoint, string p256dh, string auth);
        Task<bool> UnsubscribeUserAsync(int userId, string endpoint);
        Task<List<string>> GetUserDeviceTokensAsync(int userId);
    }
}


