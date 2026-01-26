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
            _logger.LogInformation("Starting to send confirmation email to {Email}", email);

            var subject = "Netzr - Verify Your Email";
            var htmlMessage = GenerateConfirmationEmailTemplate(user.FirstName ?? "User", confirmationLink);

            await _emailSender.SendEmailAsync(email, subject, htmlMessage);

            _logger.LogInformation("Confirmation email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", email);
            throw;
        }
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        try
        {
            _logger.LogInformation("Starting to send password reset code to {Email}", email);

            var subject = "Netzr - Password Reset Code";
            var htmlMessage = GeneratePasswordResetCodeTemplate(user.FirstName ?? "User", resetCode);

            await _emailSender.SendEmailAsync(email, subject, htmlMessage);

            _logger.LogInformation("Password reset code sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset code to {Email}", email);
            throw;
        }
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        try
        {
            _logger.LogInformation("Starting to send password reset link to {Email}", email);

            var subject = "Netzr - Reset Your Password";
            var htmlMessage = GeneratePasswordResetLinkTemplate(user.FirstName ?? "User", resetLink);

            await _emailSender.SendEmailAsync(email, subject, htmlMessage);

            _logger.LogInformation("Password reset link sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset link to {Email}", email);
            throw;
        }
    }

    private static string GenerateConfirmationEmailTemplate(string userName, string confirmationLink)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Verify Your Email</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f5f5f5;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; background-color: #f5f5f5;"">
        <tr>
            <td style=""padding: 20px 10px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; width: 100%; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #00A270; padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 22px; font-weight: 600;"">Netzr</h1>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 30px 20px; background-color: #ffffff;"">
                            <h2 style=""margin: 0 0 20px 0; color: #00A270; font-size: 18px; text-align: center;"">Verify Your Email Address</h2>
                            <p style=""margin: 0 0 15px 0; color: #333333; font-size: 15px; line-height: 1.6;"">Hello {userName},</p>
                            <p style=""margin: 0 0 15px 0; color: #333333; font-size: 15px; line-height: 1.6;"">
                                Welcome to Netzr! Please verify your email address to complete your registration and start monitoring your energy system.
                            </p>
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; background-color: #f0faf6; border-left: 4px solid #00A270; margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 15px;"">
                                        <p style=""margin: 0; color: #333333; font-size: 14px; line-height: 1.5;"">
                                            Click the button below to verify your email and activate your account.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                            <!-- CTA Button -->
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; margin: 25px 0;"">
                                <tr>
                                    <td style=""text-align: center;"">
                                        <a href=""{confirmationLink}"" style=""display: inline-block; background-color: #00A270; color: #ffffff; text-decoration: none; padding: 14px 35px; border-radius: 5px; font-size: 15px; font-weight: 600;"">Verify Email Address</a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""margin: 20px 0 0 0; color: #666666; font-size: 13px; line-height: 1.5;"">
                                If you did not create an account with Netzr, you can safely ignore this email.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 20px; text-align: center;"">
                            <p style=""margin: 0 0 10px 0; color: #666666; font-size: 13px;"">This email was sent automatically. Please do not reply.</p>
                            <p style=""margin: 0; color: #888888; font-size: 12px;"">&copy; 2025 Netzr. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
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
    <title>Password Reset Code</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f5f5f5;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; background-color: #f5f5f5;"">
        <tr>
            <td style=""padding: 20px 10px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; width: 100%; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #00A270; padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 22px; font-weight: 600;"">Netzr</h1>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 30px 20px; background-color: #ffffff;"">
                            <h2 style=""margin: 0 0 20px 0; color: #00A270; font-size: 18px; text-align: center;"">Password Reset Request</h2>
                            <p style=""margin: 0 0 15px 0; color: #333333; font-size: 15px; line-height: 1.6;"">Hello {userName},</p>
                            <p style=""margin: 0 0 15px 0; color: #333333; font-size: 15px; line-height: 1.6;"">
                                You have requested to reset your password. Use the code below to complete the process:
                            </p>
                            <!-- Code Box -->
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; background-color: #f8f9fa; border: 2px dashed #00A270; border-radius: 8px; margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 25px; text-align: center;"">
                                        <p style=""margin: 0 0 10px 0; color: #666666; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;"">Reset Code</p>
                                        <p style=""margin: 0; color: #00A270; font-size: 32px; font-weight: bold; letter-spacing: 6px; font-family: 'Courier New', monospace;"">{resetCode}</p>
                                    </td>
                                </tr>
                            </table>
                            <p style=""margin: 20px 0; color: #666666; font-size: 13px; line-height: 1.5;"">
                                <strong>This code is valid for 15 minutes.</strong>
                            </p>
                            <p style=""margin: 0; color: #666666; font-size: 13px; line-height: 1.5;"">
                                If you did not request a password reset, please ignore this email or contact support if you have concerns.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 20px; text-align: center;"">
                            <p style=""margin: 0 0 10px 0; color: #666666; font-size: 13px;"">This email was sent automatically. Please do not reply.</p>
                            <p style=""margin: 0; color: #888888; font-size: 12px;"">&copy; 2025 Netzr. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
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
    <title>Reset Your Password</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f5f5f5;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; background-color: #f5f5f5;"">
        <tr>
            <td style=""padding: 20px 10px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; width: 100%; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #00A270; padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 22px; font-weight: 600;"">Netzr</h1>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 30px 20px; background-color: #ffffff;"">
                            <h2 style=""margin: 0 0 20px 0; color: #00A270; font-size: 18px; text-align: center;"">Reset Your Password</h2>
                            <p style=""margin: 0 0 15px 0; color: #333333; font-size: 15px; line-height: 1.6;"">Hello {userName},</p>
                            <p style=""margin: 0 0 15px 0; color: #333333; font-size: 15px; line-height: 1.6;"">
                                You have requested to reset your password. Click the button below to create a new password:
                            </p>
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; background-color: #f0faf6; border-left: 4px solid #00A270; margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 15px;"">
                                        <p style=""margin: 0; color: #333333; font-size: 14px; line-height: 1.5;"">
                                            <strong>This link is valid for 15 minutes.</strong> After that, you'll need to request a new password reset.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                            <!-- CTA Button -->
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; margin: 25px 0;"">
                                <tr>
                                    <td style=""text-align: center;"">
                                        <a href=""{resetLink}"" style=""display: inline-block; background-color: #00A270; color: #ffffff; text-decoration: none; padding: 14px 35px; border-radius: 5px; font-size: 15px; font-weight: 600;"">Reset Password</a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""margin: 0; color: #666666; font-size: 13px; line-height: 1.5;"">
                                If you did not request a password reset, please ignore this email or contact support if you have concerns.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 20px; text-align: center;"">
                            <p style=""margin: 0 0 10px 0; color: #666666; font-size: 13px;"">This email was sent automatically. Please do not reply.</p>
                            <p style=""margin: 0; color: #888888; font-size: 12px;"">&copy; 2025 Netzr. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
