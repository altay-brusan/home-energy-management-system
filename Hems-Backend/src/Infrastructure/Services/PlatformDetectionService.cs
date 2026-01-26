using Hems_Backend.Application.Common.Interfaces;

namespace Hems_Backend.Infrastructure.Services;

public class PlatformDetectionService : IPlatformDetectionService
{
    public DetectedPlatform DetectPlatform(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return DetectedPlatform.Desktop;

        var ua = userAgent.ToLowerInvariant();

        // iOS detection (iPhone, iPad, iPod)
        if (ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("ipod"))
            return DetectedPlatform.iOS;

        // Android detection
        if (ua.Contains("android"))
            return DetectedPlatform.Android;

        // Default to desktop for everything else
        return DetectedPlatform.Desktop;
    }

    public bool IsInAppBrowser(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return false;

        var ua = userAgent.ToLowerInvariant();

        // Gmail app browser
        if (ua.Contains("gsa/") || ua.Contains("gmail"))
            return true;

        // Facebook app browser
        if (ua.Contains("fban") || ua.Contains("fbav") || ua.Contains("fb_iab"))
            return true;

        // Instagram app browser
        if (ua.Contains("instagram"))
            return true;

        // Twitter app browser
        if (ua.Contains("twitter"))
            return true;

        // LinkedIn app browser
        if (ua.Contains("linkedinapp"))
            return true;

        // WhatsApp browser
        if (ua.Contains("whatsapp"))
            return true;

        // Line app browser
        if (ua.Contains("line/"))
            return true;

        // Snapchat browser
        if (ua.Contains("snapchat"))
            return true;

        // Pinterest browser
        if (ua.Contains("pinterest"))
            return true;

        // WeChat browser
        if (ua.Contains("micromessenger") || ua.Contains("wechat"))
            return true;

        // TikTok browser
        if (ua.Contains("bytedance") || ua.Contains("tiktok"))
            return true;

        // Outlook app browser (iOS/Android)
        if (ua.Contains("outlookios") || ua.Contains("outlook-ios") || ua.Contains("outlook-android"))
            return true;

        return false;
    }
}
