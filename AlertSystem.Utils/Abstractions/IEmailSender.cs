namespace AlertSystem.Utils.Abstractions
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string textBody);
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendHtmlEmailAsync(string toEmail, string subject, string htmlContent);
    }
}


