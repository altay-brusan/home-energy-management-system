using Hems_Backend.Domain.Common;
using Hems_Backend.Domain.Enums;

namespace Hems_Backend.Domain.Entities;

public class MobileAppVersion : BaseGuidAuditableEntity
{
    public MobilePlatform Platform { get; set; }
    public string Version { get; set; } = string.Empty;
    public int VersionMajor { get; set; }
    public int VersionMinor { get; set; }
    public int VersionPatch { get; set; }
    public int? BuildNumber { get; set; }
    public string? StoreUrl { get; set; }
    public string? ReleaseNotes { get; set; }
    public bool IsMandatory { get; set; } = false;
    public string? MinimumRequiredVersion { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? RevocationReason { get; set; }
    public bool IsLatest { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? ReleasedAt { get; set; }
}
