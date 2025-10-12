using Hems_Backend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Hems_Backend.Infrastructure.Services;

public class InvitationEmailService : IInvitationEmailService
{
    private readonly IEmailSender _emailSender;

    public InvitationEmailService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task SendInvitationEmailAsync(
        string email, 
        string ownerName, 
        string siteName, 
        string token, 
        CancellationToken cancellationToken = default)
    {
        var subject = $"{ownerName} size {siteName} evini paylaştı";
        
        var htmlMessage = GenerateInvitationEmailTemplate(ownerName, siteName, token);

        await _emailSender.SendEmailAsync(email, subject, htmlMessage);
    }

    private static string GenerateInvitationEmailTemplate(string ownerName, string siteName, string token)
    {
        return $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Ev Paylaşım Daveti</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #ffffff;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }}
        .content {{
            padding: 40px 30px;
            color: #333333;
        }}
        .content p {{
            line-height: 1.6;
            margin: 15px 0;
            font-size: 16px;
        }}
        .token-box {{
            background-color: #f8f9fa;
            border: 2px dashed #667eea;
            border-radius: 8px;
            padding: 25px;
            text-align: center;
            margin: 30px 0;
        }}
        .token-label {{
            color: #666;
            font-size: 14px;
            margin-bottom: 10px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        .token {{
            font-size: 36px;
            font-weight: bold;
            color: #667eea;
            letter-spacing: 8px;
            font-family: 'Courier New', monospace;
        }}
        .cta-button {{
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #ffffff;
            text-decoration: none;
            padding: 15px 40px;
            border-radius: 5px;
            font-weight: 600;
            margin-top: 20px;
            transition: transform 0.2s;
        }}
        .cta-button:hover {{
            transform: translateY(-2px);
        }}
        .info-box {{
            background-color: #e7f3ff;
            border-left: 4px solid #667eea;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            color: #666;
            font-size: 14px;
        }}
        .highlight {{
            color: #667eea;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏠 Ev Paylaşım Daveti</h1>
        </div>
        <div class='content'>
            <p>Merhaba,</p>
            <p>
                <span class='highlight'>{ownerName}</span> sizinle 
                <span class='highlight'>{siteName}</span> evini paylaştı.
            </p>
            <div class='info-box'>
                <p style='margin: 0;'>
                    Bu paylaşımı kabul ederek eve ait cihazları görüntüleyebilir ve 
                    enerji tüketim verilerine erişebilirsiniz.
                </p>
            </div>
            <p>
                Paylaşımı kabul etmek için mobil uygulamaya giriş yapın ve 
                aşağıdaki <strong>6 haneli kodu</strong> kullanın:
            </p>
            <div class='token-box'>
                <div class='token-label'>Davet Kodu</div>
                <div class='token'>{token}</div>
            </div>
            <p style='color: #666; font-size: 14px;'>
                ⏰ Bu kod 10 dakika içerisinde benzersizdir ve güvenlik amacıyla 
                her davetiye için farklı bir kod oluşturulmaktadır.
            </p>
            <div style='text-align: center;'>
                <a href='#' class='cta-button'>Uygulamaya Git</a>
            </div>
        </div>
        <div class='footer'>
            <p>Bu daveti kabul etmek istemiyorsanız, bu e-postayı görmezden gelebilirsiniz.</p>
            <p style='margin-top: 10px;'>© 2025 Residential EMS. Tüm hakları saklıdır.</p>
        </div>
    </div>
</body>
</html>";
    }
}

