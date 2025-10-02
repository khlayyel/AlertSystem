using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AlertSystem.Services
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _cfg;
        private readonly ILogger<SmtpEmailSender> _logger;
        public SmtpEmailSender(IConfiguration cfg, ILogger<SmtpEmailSender> logger){ _cfg = cfg; _logger = logger; }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            await SendAsync(toEmail, subject, message);
        }

        public async Task SendAsync(string toEmail, string subject, string textBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_cfg["Smtp:FromName"] ?? "AlertSystem", _cfg["Smtp:From"] ?? "no-reply@example.com"));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = textBody };

            using var smtp = new SmtpClient();
            var host = _cfg["Smtp:Host"] ?? string.Empty;
            var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 587;
            var useStartTls = string.Equals(_cfg["Smtp:UseStartTls"], "true", StringComparison.OrdinalIgnoreCase);
            try
            {
                await smtp.ConnectAsync(host, port, useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                var user = _cfg["Smtp:User"];
                var pass = _cfg["Smtp:Pass"];
                if (!string.IsNullOrEmpty(user)) await smtp.AuthenticateAsync(user, pass);
                await smtp.SendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP send failed (Host={Host}, Port={Port}, From={From})", host, port, _cfg["Smtp:From"]);
                throw;
            }
            finally
            {
                try { await smtp.DisconnectAsync(true); } catch {}
            }
        }
    }
}


