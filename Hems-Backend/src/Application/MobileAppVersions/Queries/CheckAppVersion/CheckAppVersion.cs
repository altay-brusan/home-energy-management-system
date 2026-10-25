using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.MobileAppVersions.Queries.CheckAppVersion;

public record CheckAppVersionQuery : IRequest<AppVersionCheckResultDto>
{
    public MobilePlatform Platform { get; init; }
    public string Version { get; init; } = string.Empty;
}

public class CheckAppVersionQueryHandler : IRequestHandler<CheckAppVersionQuery, AppVersionCheckResultDto>
{
    private readonly IApplicationDbContext _context;

    public CheckAppVersionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppVersionCheckResultDto> Handle(CheckAppVersionQuery request, CancellationToken cancellationToken)
    {
        var (major, minor, patch) = ParseVersion(request.Version);

        // Get the latest version for this platform
        var latestVersion = await _context.MobileAppVersions
            .Where(v => v.Platform == request.Platform && v.IsLatest && v.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        // Check if the current version exists and if it's revoked
        var currentVersion = await _context.MobileAppVersions
            .Where(v => v.Platform == request.Platform &&
                       v.Version == request.Version &&
                       v.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        var result = new AppVersionCheckResultDto
        {
            Version = latestVersion?.Version ?? request.Version,
            IsUpdateRequired = false,
            IsUpdateAvailable = false,
            StoreUrl = latestVersion?.StoreUrl,
            Description = latestVersion?.ReleaseNotes
        };

        // No latest version found - return current version info
        if (latestVersion == null)
        {
            return result;
        }

        // Check if the current version is revoked
        if (currentVersion?.IsRevoked == true)
        {
            result.IsUpdateRequired = true;
            result.RevocationReason = currentVersion.RevocationReason;
            return result;
        }

        // Check if update is available (current < latest)
        var isOlderThanLatest = CompareVersions(major, minor, patch,
            latestVersion.VersionMajor, latestVersion.VersionMinor, latestVersion.VersionPatch) < 0;

        result.IsUpdateAvailable = isOlderThanLatest;

        if (!isOlderThanLatest)
        {
            return result;
        }

        // Check if update is required based on MinimumRequiredVersion
        if (!string.IsNullOrEmpty(latestVersion.MinimumRequiredVersion))
        {
            var (minMajor, minMinor, minPatch) = ParseVersion(latestVersion.MinimumRequiredVersion);
            if (CompareVersions(major, minor, patch, minMajor, minMinor, minPatch) < 0)
            {
                result.IsUpdateRequired = true;
                return result;
            }
        }

        // Check if any version between current and latest has IsMandatory = true
        var hasMandatoryVersionInBetween = await _context.MobileAppVersions
            .Where(v => v.Platform == request.Platform &&
                       v.IsActive &&
                       v.IsMandatory &&
                       // Version is greater than current
                       (v.VersionMajor > major ||
                        (v.VersionMajor == major && v.VersionMinor > minor) ||
                        (v.VersionMajor == major && v.VersionMinor == minor && v.VersionPatch > patch)) &&
                       // Version is less than or equal to latest
                       (v.VersionMajor < latestVersion.VersionMajor ||
                        (v.VersionMajor == latestVersion.VersionMajor && v.VersionMinor < latestVersion.VersionMinor) ||
                        (v.VersionMajor == latestVersion.VersionMajor && v.VersionMinor == latestVersion.VersionMinor && v.VersionPatch <= latestVersion.VersionPatch)))
            .AnyAsync(cancellationToken);

        if (hasMandatoryVersionInBetween)
        {
            result.IsUpdateRequired = true;
        }

        return result;
    }

    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        var parts = version.Split('.');
        var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;
        return (major, minor, patch);
    }

    private static int CompareVersions(int major1, int minor1, int patch1, int major2, int minor2, int patch2)
    {
        if (major1 != major2) return major1.CompareTo(major2);
        if (minor1 != minor2) return minor1.CompareTo(minor2);
        return patch1.CompareTo(patch2);
    }
}
