using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Hems_Backend.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> smtpSettings, ILogger<SmtpEmailSender> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            _logger.LogInformation("Starting to send email to {Email}", email);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
            message.To.Add(new MailboxAddress(email, email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Determine the secure socket option based on port and SSL setting
            SecureSocketOptions socketOptions;
            if (_smtpSettings.Port == 465)
            {
                // Port 465 uses implicit SSL (connection starts with SSL)
                socketOptions = SecureSocketOptions.SslOnConnect;
            }
            else if (_smtpSettings.Port == 587 && _smtpSettings.EnableSsl)
            {
                // Port 587 uses STARTTLS (connection starts plain, then upgrades)
                socketOptions = SecureSocketOptions.StartTls;
            }
            else if (_smtpSettings.EnableSsl)
            {
                // Let MailKit auto-detect
                socketOptions = SecureSocketOptions.Auto;
            }
            else
            {
                // No SSL
                socketOptions = SecureSocketOptions.None;
            }

            _logger.LogDebug("Connecting to SMTP server {Server}:{Port} with {SocketOptions}",
                _smtpSettings.SmtpServer, _smtpSettings.Port, socketOptions);

            await client.ConnectAsync(_smtpSettings.SmtpServer, _smtpSettings.Port, socketOptions);

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(_smtpSettings.Username) && !string.IsNullOrEmpty(_smtpSettings.Password))
            {
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}. Server: {Server}:{Port}, EnableSsl: {EnableSsl}",
                email, _smtpSettings.SmtpServer, _smtpSettings.Port, _smtpSettings.EnableSsl);
            throw;
        }
    }
}
