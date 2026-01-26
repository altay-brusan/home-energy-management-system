using Hems_Backend.Domain.Enums;

namespace Hems_Backend.Application.MobileAppVersions.Queries.GetMobileAppVersions;

public class MobileAppVersionDto
{
    public Guid Id { get; set; }
    public MobilePlatform Platform { get; set; }
    public string Version { get; set; } = string.Empty;
    public int VersionMajor { get; set; }
    public int VersionMinor { get; set; }
    public int VersionPatch { get; set; }
    public int? BuildNumber { get; set; }
    public string? StoreUrl { get; set; }
    public string? ReleaseNotes { get; set; }
    public bool IsMandatory { get; set; }
    public string? MinimumRequiredVersion { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevocationReason { get; set; }
    public bool IsLatest { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset LastModified { get; set; }
}
