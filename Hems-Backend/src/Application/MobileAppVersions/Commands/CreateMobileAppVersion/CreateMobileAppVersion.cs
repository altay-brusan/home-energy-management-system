using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.MobileAppVersions.Commands.CreateMobileAppVersion;

public record CreateMobileAppVersionCommand : IRequest<Guid>
{
    public MobilePlatform Platform { get; init; }
    public string Version { get; init; } = string.Empty;
    public int? BuildNumber { get; init; }
    public string? StoreUrl { get; init; }
    public string? ReleaseNotes { get; init; }
    public bool IsMandatory { get; init; } = false;
    public string? MinimumRequiredVersion { get; init; }
    public bool IsLatest { get; init; } = false;
    public DateTimeOffset? ReleasedAt { get; init; }
}

public class CreateMobileAppVersionCommandHandler : IRequestHandler<CreateMobileAppVersionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateMobileAppVersionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateMobileAppVersionCommand request, CancellationToken cancellationToken)
    {
        var (major, minor, patch) = ParseVersion(request.Version);

        // Check if version already exists for this platform
        var existingVersion = await _context.MobileAppVersions
            .AnyAsync(v => v.Platform == request.Platform && v.Version == request.Version, cancellationToken);

        if (existingVersion)
        {
            throw AppException.BadRequest(
                MessageCodes.VERSION_ALREADY_EXISTS,
                new Dictionary<string, object> { ["version"] = request.Version, ["platform"] = request.Platform.ToString() });
        }

        // If this is marked as latest, unmark other versions for this platform
        if (request.IsLatest)
        {
            var currentLatest = await _context.MobileAppVersions
                .Where(v => v.Platform == request.Platform && v.IsLatest)
                .ToListAsync(cancellationToken);

            foreach (var v in currentLatest)
            {
                v.IsLatest = false;
            }
        }

        var entity = new MobileAppVersion
        {
            Id = Guid.NewGuid(),
            Platform = request.Platform,
            Version = request.Version,
            VersionMajor = major,
            VersionMinor = minor,
            VersionPatch = patch,
            BuildNumber = request.BuildNumber,
            StoreUrl = request.StoreUrl,
            ReleaseNotes = request.ReleaseNotes,
            IsMandatory = request.IsMandatory,
            MinimumRequiredVersion = request.MinimumRequiredVersion,
            IsLatest = request.IsLatest,
            IsActive = true,
            ReleasedAt = request.ReleasedAt
        };

        _context.MobileAppVersions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        var parts = version.Split('.');
        var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;
        return (major, minor, patch);
    }
}
