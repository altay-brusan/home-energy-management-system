using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Services;
using Hems_Backend.Web.Infrastructure;
using Microsoft.Extensions.Options;

namespace Hems_Backend.Web.Endpoints;

public class Invite : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGet("/invite", (
            HttpContext httpContext,
            IPlatformDetectionService platformDetectionService,
            IOptions<AppLinksSettings> appLinksSettings) =>
        {
            var settings = appLinksSettings.Value;
            var inviteCode = httpContext.Request.Query["inviteCode"].ToString();
            var safeCode = string.IsNullOrEmpty(inviteCode) ? string.Empty : Uri.EscapeDataString(inviteCode);

            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var platform = platformDetectionService.DetectPlatform(userAgent);
            var isInAppBrowser = platformDetectionService.IsInAppBrowser(userAgent);

            // Deep link URL (custom scheme) - must match AndroidManifest.xml in mobile app
            var deepLinkUrl = $"n2nenergymobile://invite?inviteCode={safeCode}";

            // Android intent URL with Play Store fallback
            // Package name must match applicationId in mobile app's build.gradle
            var playStoreUrl = settings.AndroidPlayStoreUrl;
            var androidIntentUrl = string.IsNullOrEmpty(playStoreUrl)
                ? $"intent://invite?inviteCode={safeCode}#Intent;scheme=n2nenergymobile;package=com.n2nenergymobile;end"
                : $"intent://invite?inviteCode={safeCode}#Intent;scheme=n2nenergymobile;package=com.n2nenergymobile;S.browser_fallback_url={Uri.EscapeDataString(playStoreUrl)};end";

            // Build platform-specific HTML
            var html = BuildSmartRedirectHtml(
                platform,
                isInAppBrowser,
                deepLinkUrl,
                androidIntentUrl,
                settings);

            return Results.Content(html, "text/html; charset=utf-8");
        });
    }

    private static string BuildSmartRedirectHtml(
        DetectedPlatform platform,
        bool isInAppBrowser,
        string deepLinkUrl,
        string androidIntentUrl,
        AppLinksSettings settings)
    {
        var platformClass = platform switch
        {
            DetectedPlatform.iOS => "ios",
            DetectedPlatform.Android => "android",
            _ => "desktop"
        };

        var iosSmartBannerMeta = !string.IsNullOrEmpty(settings.IosBundleId)
            ? $"<meta name=\"apple-itunes-app\" content=\"app-id={settings.IosBundleId}, app-argument={deepLinkUrl}\" />"
            : string.Empty;

        var inAppBrowserWarning = isInAppBrowser
            ? @"<div class=""warning"">
                <strong>In-app Browser Detected</strong>
                <p>For the best experience, please open this link in Safari (iOS) or Chrome (Android).</p>
                <p>Tap the menu icon and select ""Open in Browser"".</p>
              </div>"
            : string.Empty;

        var platformButtons = BuildPlatformButtons(platform, deepLinkUrl, androidIntentUrl, settings);
        var autoRedirectScript = BuildAutoRedirectScript(platform, isInAppBrowser, deepLinkUrl, androidIntentUrl, settings);

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>Netzr - Accept Invitation</title>
  {iosSmartBannerMeta}
  <style>
    * {{ box-sizing: border-box; }}
    body {{
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
      margin: 0;
      padding: 24px;
      background: linear-gradient(135deg, #00A270 0%, #007a54 100%);
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
    }}
    .container {{
      max-width: 400px;
      width: 100%;
      background: #fff;
      border-radius: 16px;
      padding: 32px 24px;
      box-shadow: 0 10px 40px rgba(0,0,0,0.2);
      text-align: center;
    }}
    .logo {{
      width: 64px;
      height: 64px;
      background: linear-gradient(135deg, #00A270 0%, #007a54 100%);
      border-radius: 16px;
      margin: 0 auto 24px;
      display: flex;
      align-items: center;
      justify-content: center;
    }}
    .logo svg {{
      width: 36px;
      height: 36px;
      fill: white;
    }}
    h1 {{
      font-size: 24px;
      margin: 0 0 8px;
      color: #1a1a2e;
    }}
    .subtitle {{
      color: #666;
      margin: 0 0 24px;
      font-size: 15px;
    }}
    .loading {{
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      color: #666;
      margin-bottom: 20px;
    }}
    .spinner {{
      width: 20px;
      height: 20px;
      border: 2px solid #e0e0e0;
      border-top-color: #00A270;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }}
    @keyframes spin {{
      to {{ transform: rotate(360deg); }}
    }}
    .btn {{
      display: block;
      width: 100%;
      padding: 14px 20px;
      border: none;
      border-radius: 10px;
      font-size: 16px;
      font-weight: 600;
      text-decoration: none;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
      margin-bottom: 12px;
    }}
    .btn:hover {{
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    }}
    .btn-primary {{
      background: linear-gradient(135deg, #00A270 0%, #007a54 100%);
      color: #fff;
    }}
    .btn-secondary {{
      background: #f5f5f5;
      color: #333;
    }}
    .btn-store {{
      background: #000;
      color: #fff;
    }}
    .warning {{
      background: #fff3cd;
      border: 1px solid #ffc107;
      border-radius: 10px;
      padding: 16px;
      margin-bottom: 20px;
      text-align: left;
    }}
    .warning strong {{
      display: block;
      margin-bottom: 8px;
      color: #856404;
    }}
    .warning p {{
      margin: 4px 0;
      font-size: 13px;
      color: #856404;
    }}
    .info-box {{
      background: #e8f5f0;
      border: 1px solid #00A270;
      border-radius: 10px;
      padding: 16px;
      margin-bottom: 20px;
      text-align: left;
    }}
    .info-box strong {{
      display: block;
      margin-bottom: 8px;
      color: #007a54;
    }}
    .info-box p {{
      margin: 4px 0;
      font-size: 13px;
      color: #007a54;
    }}
    .hidden {{ display: none; }}
    .platform-ios .android-only {{ display: none; }}
    .platform-android .ios-only {{ display: none; }}
    .platform-desktop .mobile-only {{ display: none; }}
    .platform-ios .desktop-only,
    .platform-android .desktop-only {{ display: none; }}
    noscript .loading {{ display: none; }}
  </style>
</head>
<body class=""platform-{platformClass}"">
  <div class=""container"">
    <div class=""logo"">
      <svg viewBox=""0 0 24 24"" xmlns=""http://www.w3.org/2000/svg"">
        <path d=""M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5""/>
      </svg>
    </div>

    <h1>You're Invited!</h1>
    <p class=""subtitle mobile-only"">Open the Netzr app to accept your invitation</p>
    <p class=""subtitle desktop-only"">Redirecting to web dashboard...</p>

    {inAppBrowserWarning}

    <div class=""loading mobile-only"" id=""loading"">
      <div class=""spinner""></div>
      <span>Opening app...</span>
    </div>

    {platformButtons}

    <noscript>
      <p style=""color:#666;margin-top:20px;font-size:14px;"">
        JavaScript is disabled. Please copy the code above and enter it manually in the app.
      </p>
    </noscript>
  </div>

  {autoRedirectScript}
</body>
</html>";
    }

    private static string BuildPlatformButtons(
        DetectedPlatform platform,
        string deepLinkUrl,
        string androidIntentUrl,
        AppLinksSettings settings)
    {
        // For Android, use Intent URL (more reliable, includes fallback to store)
        // For iOS, use custom scheme
        var appLinkUrl = platform == DetectedPlatform.Android ? androidIntentUrl : deepLinkUrl;
        var openInAppButton = $@"<a href=""{appLinkUrl}"" class=""btn btn-primary mobile-only"" id=""openAppBtn"">Open in App</a>";

        var androidPlayStoreButton = !string.IsNullOrEmpty(settings.AndroidPlayStoreUrl)
            ? $@"<a href=""{settings.AndroidPlayStoreUrl}"" class=""btn btn-store android-only"" id=""playStoreBtn"">Get it on Google Play</a>"
            : string.Empty;

        var iosAppStoreButton = !string.IsNullOrEmpty(settings.IosAppStoreUrl)
            ? $@"<a href=""{settings.IosAppStoreUrl}"" class=""btn btn-store ios-only"" id=""appStoreBtn"">Download on App Store</a>"
            : @"<p class=""ios-only"" style=""color:#666;font-size:13px;margin-top:8px;"">iOS app coming soon</p>";

        // Desktop users are redirected to web dashboard automatically (see BuildAutoRedirectScript)
        // Show a manual button as fallback in case auto-redirect fails
        var desktopButton = !string.IsNullOrEmpty(settings.WebDashboardUrl)
            ? $@"<a href=""{settings.WebDashboardUrl}"" class=""btn btn-primary desktop-only"">Open Web Dashboard</a>"
            : string.Empty;

        return $@"
    {openInAppButton}
    {androidPlayStoreButton}
    {iosAppStoreButton}
    {desktopButton}";
    }

    private static string BuildAutoRedirectScript(
        DetectedPlatform platform,
        bool isInAppBrowser,
        string deepLinkUrl,
        string androidIntentUrl,
        AppLinksSettings settings)
    {
        // Desktop users are redirected to web dashboard
        // TODO: Make WebDashboardUrl environment-specific in the future:
        //   - Dev: https://hems-app-dev.n2nenergy.com/dashboard (or similar)
        //   - Stage: https://hems-app-stg.n2nenergy.com/dashboard (or similar)
        //   - Prod: https://hems-app.n2nenergy.com/dashboard
        // Currently using the same URL for all environments (configured in appsettings.json)
        if (platform == DetectedPlatform.Desktop)
        {
            if (!string.IsNullOrEmpty(settings.WebDashboardUrl))
            {
                return $@"
  <script>
    // Desktop: redirect to web dashboard after a brief delay
    setTimeout(function() {{
      window.location.href = '{settings.WebDashboardUrl}';
    }}, 2000);
  </script>";
            }
            return string.Empty;
        }

        // Don't auto-redirect in in-app browsers (they often block deep links)
        if (isInAppBrowser)
        {
            return @"
  <script>
    // In-app browser detected - hide loading indicator
    document.getElementById('loading').style.display = 'none';
  </script>";
        }

        return $@"
  <script>
    (function() {{
      var deepLink = '{deepLinkUrl}';
      var intentUrl = '{androidIntentUrl}';
      var platform = '{platform.ToString().ToLower()}';
      var redirectAttempted = false;

      function hideLoading() {{
        var loading = document.getElementById('loading');
        if (loading) loading.style.display = 'none';
      }}

      function attemptDeepLink() {{
        if (redirectAttempted) return;
        redirectAttempted = true;

        // Try the deep link
        if (platform === 'android') {{
          // On Android, try intent URL first
          window.location.href = intentUrl;
        }} else {{
          // On iOS, use custom scheme
          window.location.href = deepLink;
        }}

        // After a delay, hide loading if we're still on the page
        setTimeout(function() {{
          hideLoading();
        }}, 2500);
      }}

      // Attempt deep link after a brief delay to allow page to render
      setTimeout(attemptDeepLink, 500);

      // Handle visibility change (user might come back from app)
      document.addEventListener('visibilitychange', function() {{
        if (document.visibilityState === 'visible') {{
          hideLoading();
        }}
      }});
    }})();
  </script>";
    }
}
