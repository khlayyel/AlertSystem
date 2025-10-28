using System;
using System.Text;
using System.IO;
using System.Linq;

namespace AlertSystem.Service.Services
{
    public interface IEmailTemplateService
    {
        string CreateAlertEmailTemplate(string title, string message, string senderName, DateTime timestamp, string confirmationUrl);
    }

    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly string _templatePath;

        public EmailTemplateService()
        {
            // Try to find the template in multiple possible locations
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "AlertNotification.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "AlertNotification.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "AlertSystem.WEB", "EmailTemplates", "AlertNotification.html")
            };

            _templatePath = possiblePaths.FirstOrDefault(File.Exists);
            
            if (string.IsNullOrEmpty(_templatePath))
            {
                throw new FileNotFoundException("AlertNotification.html template not found in any of the expected locations.");
            }
        }

        public string CreateAlertEmailTemplate(string title, string message, string senderName, DateTime timestamp, string confirmationUrl)
        {
            try
            {
                // Load the external template
                var template = File.ReadAllText(_templatePath);
                
                // Replace placeholders with actual values
                var html = template
                    .Replace("{{TITLE}}", title)
                    .Replace("{{MESSAGE}}", message.Replace("\n", "<br>"))
                    .Replace("{{SENDER_NAME}}", senderName)
                    .Replace("{{TIMESTAMP}}", timestamp.ToString("dd/MM/yyyy HH:mm:ss"))
                    .Replace("{{CONFIRMATION_URL}}", confirmationUrl);

                return html;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load or process email template: {ex.Message}", ex);
            }
        }
    }
}
