using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Worker.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _smtpFrom;
        private readonly string _smtpFromName;

        public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration)
        {
            _logger = logger;
            _smtpHost = configuration["Smtp:Host"] ?? "";
            _smtpPort = int.TryParse(configuration["Smtp:Port"], out var p) ? p : 587;
            _smtpUser = configuration["Smtp:User"] ?? "";
            _smtpPass = configuration["Smtp:Pass"] ?? "";
            _smtpFrom = configuration["Smtp:From"] ?? _smtpUser;
            _smtpFromName = configuration["Smtp:FromName"] ?? "AlertSystem";
        }

        public async Task SendAsync(string email, string fullName, string title, string message, CancellationToken cancellationToken = default)
        {
            try
            {
                using var smtp = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass)
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(_smtpFrom, _smtpFromName, Encoding.UTF8),
                    Subject = $"AlertSystem: {title}",
                    Body = $"Bonjour {fullName},\n\n{message}\n\nCordialement,\nAlertSystem",
                    BodyEncoding = Encoding.UTF8
                };
                mail.To.Add(email);

                await smtp.SendMailAsync(mail, cancellationToken);
                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                throw;
            }
        }
    }
}
