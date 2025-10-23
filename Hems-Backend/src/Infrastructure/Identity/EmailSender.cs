using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Identity;

public class EmailSender : IEmailSender<ApplicationUser>
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IEmailSender emailSender, ILogger<EmailSender> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        try
        {
            _logger.LogInformation("🚀 Starting to send confirmation email to {Email}", email);
            
            var subject = "HEMS - Email Doğrulama";
            var htmlMessage = GenerateConfirmationEmailTemplate(user.FirstName ?? "Kullanıcı", confirmationLink);
            
            await _emailSender.SendEmailAsync(email, subject, htmlMessage);
            
            _logger.LogInformation("✅ Confirmation email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send confirmation email to {Email}", email);
            throw;
        }
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        try
        {
            _logger.LogInformation("🚀 Starting to send password reset code to {Email}", email);
            
            var subject = "HEMS - Password Reset Code";
            var htmlMessage = GeneratePasswordResetCodeTemplate(user.FirstName ?? "User", resetCode);
            
            await _emailSender.SendEmailAsync(email, subject, htmlMessage);
            
            _logger.LogInformation("✅ Password reset code sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send password reset code to {Email}", email);
            throw;
        }
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        try
        {
            _logger.LogInformation("🚀 Starting to send password reset link to {Email}", email);
            
            var subject = "HEMS - Password Reset Link";
            var htmlMessage = GeneratePasswordResetLinkTemplate(user.FirstName ?? "User", resetLink);
            
            await _emailSender.SendEmailAsync(email, subject, htmlMessage);
            
            _logger.LogInformation("✅ Password reset link sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send password reset link to {Email}", email);
            throw;
        }
    }

    private static string GenerateConfirmationEmailTemplate(string userName, string confirmationLink)
    {
        return $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Email Doğrulama</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; background-color: #3498db; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>HEMS - Email Doğrulama</h1>
        </div>
        <div class=""content"">
            <h2>Merhaba {userName},</h2>
            <p>HEMS sistemine hoş geldiniz! Email adresinizi doğrulamak için aşağıdaki butona tıklayın:</p>
            <p style=""text-align: center;"">
                <a href=""{confirmationLink}"" class=""button"">Email Adresimi Doğrula</a>
            </p>
            <p>Eğer bu işlemi siz yapmadıysanız, bu emaili görmezden gelebilirsiniz.</p>
        </div>
        <div class=""footer"">
            <p>Bu email otomatik olarak gönderilmiştir. Lütfen yanıtlamayın.</p>
            <p>© 2024 HEMS - Home Energy Management System</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GeneratePasswordResetCodeTemplate(string userName, string resetCode)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #e74c3c; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .code {{ background-color: #34495e; color: white; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; margin: 20px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>HEMS - Password Reset</h1>
        </div>
        <div class=""content"">
            <h2>Hello {userName},</h2>
            <p>You have requested to reset your password. Please use the following code to reset your password:</p>
            <div class=""code"">{resetCode}</div>
            <p><strong>This code is valid for 15 minutes.</strong></p>
            <p>If you did not request this password reset, please ignore this email.</p>
        </div>
        <div class=""footer"">
            <p>This email was sent automatically. Please do not reply.</p>
            <p>© 2024 HEMS - Home Energy Management System</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GeneratePasswordResetLinkTemplate(string userName, string resetLink)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #e74c3c; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; background-color: #e74c3c; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>HEMS - Password Reset</h1>
        </div>
        <div class=""content"">
            <h2>Hello {userName},</h2>
            <p>You have requested to reset your password. Please click the button below to reset your password:</p>
            <p style=""text-align: center;"">
                <a href=""{resetLink}"" class=""button"">Reset My Password</a>
            </p>
            <p><strong>This link is valid for 15 minutes.</strong></p>
            <p>If you did not request this password reset, please ignore this email.</p>
        </div>
        <div class=""footer"">
            <p>This email was sent automatically. Please do not reply.</p>
            <p>© 2024 HEMS - Home Energy Management System</p>
        </div>
    </div>
</body>
</html>";
    }
}
