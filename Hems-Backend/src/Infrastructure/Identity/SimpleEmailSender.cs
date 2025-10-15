using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Identity;

public class SimpleEmailSender : IEmailSender
{
    private readonly ILogger<SimpleEmailSender> _logger;

    public SimpleEmailSender(ILogger<SimpleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Console'a yaz
        Console.WriteLine($"📧 EMAIL SENT TO: {email}");
        Console.WriteLine($"📧 SUBJECT: {subject}");
        Console.WriteLine($"📧 CONTENT PREVIEW: {htmlMessage.Substring(0, Math.Min(200, htmlMessage.Length))}...");
        
        // Logger'a da yaz
        _logger.LogInformation("📧 Email sent to {Email} with subject: {Subject}", email, subject);
        
        return Task.CompletedTask;
    }
}











