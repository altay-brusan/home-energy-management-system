using Hems_Backend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hems_Backend.Infrastructure.Services;

public class InvitationEmailService : IInvitationEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<InvitationEmailService> _logger;
    private readonly SmtpSettings _smtpSettings;

    public InvitationEmailService(IEmailSender emailSender, ILogger<InvitationEmailService> logger, IOptions<SmtpSettings> smtpSettings)
    {
        _smtpSettings = smtpSettings.Value;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendInvitationEmailAsync(
        string email, 
        string ownerName, 
        string siteName, 
        string token, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🚀 Starting to send invitation email to {Email} for site {SiteName}", email, siteName);
            
            var subject = $"{ownerName} has invited you to access {siteName}";
            
            var htmlMessage = GenerateInvitationEmailTemplate(ownerName, siteName, token,_smtpSettings.InvitationUrl);
            Console.WriteLine(htmlMessage);
            _logger.LogInformation("📧 Sending email with subject: {Subject}", subject);
            
            await _emailSender.SendEmailAsync(email, subject, htmlMessage);
            
            _logger.LogInformation("✅ Invitation email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send invitation email to {Email}", email);
            throw; // Re-throw to let caller handle
        }
    }

    private static string GenerateInvitationEmailTemplate(string ownerName, string siteName, string token,string invetationUrl)
    {
        var escapedToken = System.Uri.EscapeDataString(token);
        var baseUrl = string.IsNullOrWhiteSpace(invetationUrl) ? "https://hems-api.n2nenergy.com" : invetationUrl.TrimEnd('/');
        var fallback = $"{baseUrl}/invite?inviteCode={escapedToken}";
        var intent = $"intent://invite?inviteCode={escapedToken}#Intent;scheme=n2nenergymobile;package=com.n2nenergymobile;S.browser_fallback_url={System.Uri.EscapeDataString(fallback)};end";

        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Device Access Invitation</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f5f5f5;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; background-color: #f5f5f5;"">
        <tr>
            <td style=""padding: 20px 10px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; width: 100%; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #00A270; padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 22px; font-weight: 600;"">Green Power Energy</h1>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 30px 20px; background-color: #ffffff;"">
                            <h2 style=""margin: 0 0 20px 0; color: #00A270; font-size: 18px; text-align: center;"">Device Access Invitation</h2>
                            <p style=""margin: 0 0 15px 0; color: #333333; font-size: 15px; line-height: 1.6;"">Hello,</p>
                            <p style=""margin: 0 0 15px 0; color: #333333; font-size: 15px; line-height: 1.6;"">
                                <strong style=""color: #00A270;"">{ownerName}</strong> has invited you to access the energy system at
                                <strong style=""color: #00A270;"">{siteName}</strong>.
                            </p>
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; background-color: #f0faf6; border-left: 4px solid #00A270; margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 15px;"">
                                        <p style=""margin: 0; color: #333333; font-size: 14px; line-height: 1.5;"">
                                            By accepting this invitation, you can monitor the hybrid inverter and view energy consumption data.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                            <p style=""margin: 0 0 20px 0; color: #333333; font-size: 15px; line-height: 1.6;"">
                                To accept the invitation, sign in to the mobile app and use the <strong>6-digit code</strong> below:
                            </p>
                            <!-- Token Box -->
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; background-color: #f8f9fa; border: 2px dashed #00A270; border-radius: 8px; margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 25px; text-align: center;"">
                                        <p style=""margin: 0 0 10px 0; color: #666666; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;"">Invitation Code</p>
                                        <p style=""margin: 0; color: #00A270; font-size: 32px; font-weight: bold; letter-spacing: 6px; font-family: 'Courier New', monospace;"">{token}</p>
                                    </td>
                                </tr>
                            </table>
                            <p style=""margin: 20px 0; color: #666666; font-size: 13px; line-height: 1.5;"">
                                This code is valid for 24 hours and is uniquely generated for each invitation.
                            </p>
                            <!-- CTA Button -->
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; margin: 25px 0;"">
                                <tr>
                                    <td style=""text-align: center;"">
                                        <a href=""{fallback}"" style=""display: inline-block; background-color: #00A270; color: #ffffff; text-decoration: none; padding: 14px 35px; border-radius: 5px; font-size: 15px; font-weight: 600;"">Open App</a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""margin: 15px 0 0 0; color: #888888; font-size: 12px; text-align: center; word-break: break-all;"">
                                Or click this link: <a href=""{fallback}"" style=""color: #00A270;"">{fallback}</a>
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 20px; text-align: center;"">
                            <p style=""margin: 0 0 10px 0; color: #666666; font-size: 13px;"">If you do not wish to accept this invitation, you can ignore this email.</p>
                            <p style=""margin: 0; color: #888888; font-size: 12px;"">© 2025 Green Power Energy. All rights reserved.</p>
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

