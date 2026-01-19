namespace Hems_Backend.Application.MobileAppVersions.Queries.CheckAppVersion;

public class AppVersionCheckResultDto
{
    public string Version { get; set; } = string.Empty;
    public bool IsUpdateRequired { get; set; }
    public bool IsUpdateAvailable { get; set; }
    public string? StoreUrl { get; set; }
    public string? Description { get; set; }
    public string? RevocationReason { get; set; }
}
