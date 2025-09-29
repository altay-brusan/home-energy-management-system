using Microsoft.AspNetCore.Identity.UI.Services;

namespace Hems_Backend.Infrastructure.Identity;

public class SimpleEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // For now, just log the email
        // In production, you would send an actual email
        Console.WriteLine($"Email to {email}: {subject}");
        Console.WriteLine($"Content: {htmlMessage}");
        return Task.CompletedTask;
    }
}









