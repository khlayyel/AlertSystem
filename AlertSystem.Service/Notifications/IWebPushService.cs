namespace AlertSystem.Services
{
    // Web Push interface lives in Service to avoid circular deps. Implemented in Infrastructure.
    public interface IWebPushService
    {
        Task<bool> SendNotificationAsync(string deviceToken, string title, string message, string? iconUrl = null, object? data = null);
        Task<string> SubscribeUserAsync(int userId, string endpoint, string p256dh, string auth);
        Task<bool> UnsubscribeUserAsync(int userId, string endpoint);
        Task<List<string>> GetUserDeviceTokensAsync(int userId);
    }
}


