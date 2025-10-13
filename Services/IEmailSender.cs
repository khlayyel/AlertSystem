namespace AlertSystem.Services
{
    // Contract for sending emails. Implementations (e.g., SmtpEmailSender) provide the actual transport.
    // Both methods exist for historical reasons; SendEmailAsync delegates to SendAsync.
    public interface IEmailSender
    {
        // Sends a plain-text email to a recipient with subject/body.
        Task SendAsync(string toEmail, string subject, string textBody);

        // Convenience alias that calls SendAsync under the hood.
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}


