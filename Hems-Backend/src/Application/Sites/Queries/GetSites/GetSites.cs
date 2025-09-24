using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Sites.Queries.GetSites;

public record GetSitesQuery : IRequest<PaginatedList<SiteDto>>
{
    public string? UserId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetSitesQueryHandler : IRequestHandler<GetSitesQuery, PaginatedList<SiteDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSitesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<SiteDto>> Handle(GetSitesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Sites.AsQueryable();

        if (!string.IsNullOrEmpty(request.UserId))
        {
            query = query.Where(x => x.UserId == request.UserId);
        }

        // Only get non-deleted sites
        query = query.Where(x => !x.IsDeleted);

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new SiteDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Name = x.Name,
                Country = x.Country,
                City = x.City,
                AddressLine = x.AddressLine,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Timezone = x.Timezone,
                IsDeleted = x.IsDeleted,
                Created = x.Created.DateTime,
                CreatedBy = x.CreatedBy,
                LastModified = x.LastModified.DateTime,
                LastModifiedBy = x.LastModifiedBy
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}

