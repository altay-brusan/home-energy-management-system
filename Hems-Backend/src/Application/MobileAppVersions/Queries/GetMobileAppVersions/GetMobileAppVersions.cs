using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.MobileAppVersions.Queries.GetMobileAppVersions;

public record GetMobileAppVersionsQuery : IRequest<PaginatedList<MobileAppVersionDto>>
{
    public MobilePlatform? Platform { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetMobileAppVersionsQueryHandler : IRequestHandler<GetMobileAppVersionsQuery, PaginatedList<MobileAppVersionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMobileAppVersionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<MobileAppVersionDto>> Handle(GetMobileAppVersionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MobileAppVersions.AsQueryable();

        if (request.Platform.HasValue)
        {
            query = query.Where(v => v.Platform == request.Platform.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(v => v.IsActive == request.IsActive.Value);
        }

        var orderedQuery = query
            .OrderByDescending(v => v.Platform)
            .ThenByDescending(v => v.VersionMajor)
            .ThenByDescending(v => v.VersionMinor)
            .ThenByDescending(v => v.VersionPatch)
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
            });

        return await PaginatedList<MobileAppVersionDto>.CreateAsync(orderedQuery, request.PageNumber, request.PageSize);
    }
}
