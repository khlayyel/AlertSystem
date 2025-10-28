namespace AlertSystem.Services
{
    /// <summary>
    /// Interface pour le service Web Push - respecte le principe ISP
    /// </summary>
    public interface IWebPushService
    {
        Task<bool> SendNotificationAsync(string deviceToken, string title, string message, string? iconUrl = null, object? data = null);
        Task<string> SubscribeUserAsync(int userId, string endpoint, string p256dh, string auth);
        Task<bool> UnsubscribeUserAsync(int userId, string endpoint);
        Task<List<string>> GetUserDeviceTokensAsync(int userId);
    }
}
