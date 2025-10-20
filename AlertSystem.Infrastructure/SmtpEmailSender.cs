using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using AlertSystem.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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

        public async Task SendHtmlEmailAsync(string toEmail, string subject, string htmlContent)
        {
            // Build an HTML email using MimeKit
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_cfg["Smtp:FromName"] ?? "AlertSystem", _cfg["Smtp:From"] ?? "no-reply@example.com"));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            
            // Create multipart message with both HTML and plain text
            var multipart = new MultipartAlternative();
            
            // Add plain text version (strip HTML tags)
            var plainText = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<[^>]*>", "");
            multipart.Add(new TextPart("plain") { Text = plainText });
            
            // Add HTML version
            multipart.Add(new TextPart("html") { Text = htmlContent });
            
            message.Body = multipart;

            using var smtp = new SmtpClient();
            var host = _cfg["Smtp:Host"] ?? string.Empty;
            var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 587;
            var useStartTls = string.Equals(_cfg["Smtp:UseStartTls"], "true", StringComparison.OrdinalIgnoreCase);
            var user = _cfg["Smtp:User"];
            var pass = _cfg["Smtp:Pass"];
            
            // Choose the right TLS mode. Gmail typically requires:
            // - Port 465: SSL on connect
            // - Port 587: STARTTLS
            SecureSocketOptions tlsOption;
            if (port == 465)
            {
                tlsOption = SecureSocketOptions.SslOnConnect;
            }
            else if (useStartTls)
            {
                tlsOption = SecureSocketOptions.StartTls;
            }
            else
            {
                tlsOption = SecureSocketOptions.Auto;
            }
            
            try
            {
                await smtp.ConnectAsync(host, port, tlsOption);
                // Authenticate if username is provided (supports Gmail/app passwords)
                if (!string.IsNullOrEmpty(user)) await smtp.AuthenticateAsync(user, pass);
                await smtp.SendAsync(message);
            }
            catch (Exception ex)
            {
                // Log full context (host/port/from) to debug connectivity/auth issues
                _logger.LogError(ex, "SMTP HTML send failed (Host={Host}, Port={Port}, From={From}, To={To})", host, port, _cfg["Smtp:From"], toEmail);
                _logger.LogError("SMTP Details: User={User}, UseStartTls={UseStartTls}, TLS={TlsOption}", user, useStartTls, tlsOption);
                throw;
            }
            finally
            {
                try { await smtp.DisconnectAsync(true); } catch {}
            }
        }

        public async Task SendAsync(string toEmail, string subject, string textBody)
        {
            // Build a simple plain-text email using MimeKit
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_cfg["Smtp:FromName"] ?? "AlertSystem", _cfg["Smtp:From"] ?? "no-reply@example.com"));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = textBody };

            using var smtp = new SmtpClient();
            var host = _cfg["Smtp:Host"] ?? string.Empty;
            var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 587;
            var useStartTls = string.Equals(_cfg["Smtp:UseStartTls"], "true", StringComparison.OrdinalIgnoreCase);
            var user = _cfg["Smtp:User"];
            var pass = _cfg["Smtp:Pass"];
            
            // Choose the right TLS mode. Gmail typically requires:
            // - Port 465: SSL on connect
            // - Port 587: STARTTLS
            SecureSocketOptions tlsOption;
            if (port == 465)
            {
                tlsOption = SecureSocketOptions.SslOnConnect;
            }
            else if (useStartTls)
            {
                tlsOption = SecureSocketOptions.StartTls;
            }
            else
            {
                tlsOption = SecureSocketOptions.Auto;
            }
            
            try
            {
                await smtp.ConnectAsync(host, port, tlsOption);
                // Authenticate if username is provided (supports Gmail/app passwords)
                if (!string.IsNullOrEmpty(user)) await smtp.AuthenticateAsync(user, pass);
                await smtp.SendAsync(message);
            }
            catch (Exception ex)
            {
                // Log full context (host/port/from) to debug connectivity/auth issues
                _logger.LogError(ex, "SMTP send failed (Host={Host}, Port={Port}, From={From}, To={To})", host, port, _cfg["Smtp:From"], toEmail);
                _logger.LogError("SMTP Details: User={User}, UseStartTls={UseStartTls}, TLS={TlsOption}", user, useStartTls, tlsOption);
                throw;
            }
            finally
            {
                try { await smtp.DisconnectAsync(true); } catch {}
            }
        }
    }
}


