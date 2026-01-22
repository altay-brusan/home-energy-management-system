namespace Hems_Backend.Application.Common.Interfaces;

public enum DetectedPlatform
{
    iOS,
    Android,
    Desktop
}

public interface IPlatformDetectionService
{
    DetectedPlatform DetectPlatform(string? userAgent);
    bool IsInAppBrowser(string? userAgent);
}
