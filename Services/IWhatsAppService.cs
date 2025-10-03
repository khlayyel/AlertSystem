namespace AlertSystem.Services
{
    public interface IWhatsAppService
    {
        Task<bool> SendMessageAsync(string phoneNumber, string message);
        Task<bool> SendAlertAsync(string phoneNumber, string title, string message, string senderName);
        Task<bool> SendTemplateHelloAsync(string phoneNumber);
    }
}