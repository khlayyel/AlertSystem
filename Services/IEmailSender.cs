namespace AlertSystem.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string textBody);
    }
}


