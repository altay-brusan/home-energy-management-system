using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.MobileAppVersions.Queries.GetMobileAppVersions;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.MobileAppVersions.Queries.GetMobileAppVersionById;

public record GetMobileAppVersionByIdQuery(Guid Id) : IRequest<MobileAppVersionDto>;

public class GetMobileAppVersionByIdQueryHandler : IRequestHandler<GetMobileAppVersionByIdQuery, MobileAppVersionDto>
{
    private readonly IApplicationDbContext _context;

    public GetMobileAppVersionByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MobileAppVersionDto> Handle(GetMobileAppVersionByIdQuery request, CancellationToken cancellationToken)
    {
        var version = await _context.MobileAppVersions
            .Where(v => v.Id == request.Id)
            .Select(v => new MobileAppVersionDto
            {
                Id = v.Id,
                Platform = v.Platform,
                Version = v.Version,
                VersionMajor = v.VersionMajor,
                VersionMinor = v.VersionMinor,
                VersionPatch = v.VersionPatch,
                BuildNumber = v.BuildNumber,
                StoreUrl = v.StoreUrl,
                ReleaseNotes = v.ReleaseNotes,
                IsMandatory = v.IsMandatory,
                MinimumRequiredVersion = v.MinimumRequiredVersion,
                IsRevoked = v.IsRevoked,
                RevocationReason = v.RevocationReason,
                IsLatest = v.IsLatest,
                IsActive = v.IsActive,
                ReleasedAt = v.ReleasedAt,
                Created = v.Created,
                LastModified = v.LastModified
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            throw new Common.Exceptions.NotFoundException(nameof(MobileAppVersion), request.Id);
        }

        return version;
    }
}
