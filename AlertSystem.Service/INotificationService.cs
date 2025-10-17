namespace AlertSystem.Services
{
    public interface INotificationService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string message);
        Task<bool> SendWhatsAppAsync(string phoneNumber, string message);
        Task<bool> SendPushNotificationAsync(int userId, string title, string message, string? url = null);
        Task<List<string>> SendToAllPlatformsAsync(int userId, string title, string message, string? url = null);
    }
}
