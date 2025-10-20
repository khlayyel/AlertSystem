using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Testing SMTP directly...");
            
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("AlertSystem", "khalilouerghemmi@gmail.com"));
            message.To.Add(MailboxAddress.Parse("zied.soltani11@gmail.com"));
            message.Subject = "Direct SMTP Test";
            message.Body = new TextPart("plain") { Text = "This is a direct SMTP test" };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync("khalilouerghemmi@gmail.com", "xiczhnsf ywjqwgvd");
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
            
            Console.WriteLine("✅ Email sent successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Email failed: {ex.Message}");
            Console.WriteLine($"Full error: {ex}");
        }
    }
}