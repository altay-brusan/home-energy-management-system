using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Hems_Backend.Infrastructure.Identity;

public class EmailSender : IEmailSender<ApplicationUser>
{
    private readonly IEmailSender _emailSender;

    public EmailSender(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        // For now, just log the confirmation link
        // In production, you would send an actual email
        Console.WriteLine($"Confirmation link for {email}: {confirmationLink}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        // For now, just log the reset code
        // In production, you would send an actual email
        Console.WriteLine($"Password reset code for {email}: {resetCode}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        // For now, just log the reset link
        // In production, you would send an actual email
        Console.WriteLine($"Password reset link for {email}: {resetLink}");
        return Task.CompletedTask;
    }
}
