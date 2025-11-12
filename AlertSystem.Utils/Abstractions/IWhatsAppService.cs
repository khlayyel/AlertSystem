namespace AlertSystem.Utils.Abstractions
{
    public interface IWhatsAppService
    {
        Task<bool> SendMessageAsync(string phoneNumber, string message);
        Task<bool> SendAlertAsync(string phoneNumber, string title, string message, string senderName);
        Task<bool> SendTemplateHelloAsync(string phoneNumber);
        Task<bool> SendTemplateAsync(string phoneNumber, string templateName, string languageCode, IDictionary<string, string>? variables = null);
    }
}


