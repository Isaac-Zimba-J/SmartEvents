using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SmartEvents.API.Application.Interfaces;

namespace SmartEvents.API.Infrastructure.Notifications;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var smtp = configuration.GetSection("SmtpSettings");
        var host = smtp["Host"];
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"];
        var password = smtp["Password"];
        var fromName = smtp["FromName"] ?? "SmartEvents";
        var fromEmail = smtp["FromEmail"] ?? username;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
        {
            logger.LogWarning("SMTP not configured. Skipping email to {To}: {Subject}", to, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}
