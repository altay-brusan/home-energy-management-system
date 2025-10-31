using Hems_Backend.Web.Infrastructure;

namespace Hems_Backend.Web.Endpoints;

public class Invite : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGet("/invite", (HttpContext httpContext) =>
        {
            var inviteCode = httpContext.Request.Query["inviteCode"].ToString();
            var safeCode = string.IsNullOrEmpty(inviteCode) ? string.Empty : Uri.EscapeDataString(inviteCode);

            // Minimal HTML. Android App Links will handle opening the app when configured.
            // If not handled, we present a page with a manual open link as a fallback.
            var html = $@"<!DOCTYPE html>
<html lang=""tr"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>Invite</title>
  <style>
    body {{ font-family: -apple-system, Segoe UI, Roboto, Arial, sans-serif; padding: 24px; color: #222; }}
    .box {{ max-width: 520px; margin: 0 auto; text-align: center; }}
    .btn {{ display:inline-block; padding:12px 20px; background:#4f46e5; color:#fff; text-decoration:none; border-radius:8px; margin-top:16px; }}
    .muted {{ color:#666; font-size:14px; margin-top:12px; }}
  </style>
</head>
<body>
  <div class=""box"">
    <h1>Daveti Aç</h1>
    <p>Uygulama yüklüyse otomatik açılacaktır.</p>
    <p><a class=""btn"" href=""n2nenergymobile://invite?inviteCode={safeCode}"">Uygulamada Aç</a></p>
    <p class=""muted"">Kod: <strong>{System.Net.WebUtility.HtmlEncode(inviteCode)}</strong></p>
  </div>
</body>
</html>";

            return Results.Content(html, "text/html; charset=utf-8");
        });
    }
}


