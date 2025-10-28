using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using AlertSystem.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AlertSystem.Utils.Email;
using AlertSystem.Utils.Logging;

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
            var config = SmtpConfigurationUtils.GetSmtpConfiguration(_cfg);
            
            // Build an HTML email using MimeKit
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
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

            using var smtp = SmtpConfigurationUtils.CreateSmtpClient(config, _logger);
            
            try
            {
                await SmtpConfigurationUtils.ConnectAndAuthenticateAsync(smtp, config, _logger);
                await smtp.SendAsync(message);
                _logger.LogServiceSuccess("SmtpEmailSender", "SendHtmlEmailAsync", 
                    $"HTML email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogServiceError("SmtpEmailSender", "SendHtmlEmailAsync", ex, new
                {
                    To = toEmail,
                    Subject = subject,
                    Host = config.Host,
                    Port = config.Port,
                    From = config.FromEmail
                });
                throw;
            }
            finally
            {
                await SmtpConfigurationUtils.SafeDisconnectAsync(smtp, _logger);
            }
        }

        public async Task SendAsync(string toEmail, string subject, string textBody)
        {
            var config = SmtpConfigurationUtils.GetSmtpConfiguration(_cfg);
            
            // Build a simple plain-text email using MimeKit
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = textBody };

            using var smtp = SmtpConfigurationUtils.CreateSmtpClient(config, _logger);
            
            try
            {
                await SmtpConfigurationUtils.ConnectAndAuthenticateAsync(smtp, config, _logger);
                await smtp.SendAsync(message);
                _logger.LogServiceSuccess("SmtpEmailSender", "SendAsync", 
                    $"Plain text email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogServiceError("SmtpEmailSender", "SendAsync", ex, new
                {
                    To = toEmail,
                    Subject = subject,
                    Host = config.Host,
                    Port = config.Port,
                    From = config.FromEmail
                });
                throw;
            }
            finally
            {
                await SmtpConfigurationUtils.SafeDisconnectAsync(smtp, _logger);
            }
        }
    }
}


