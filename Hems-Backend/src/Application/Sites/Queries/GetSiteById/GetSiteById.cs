using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Sites.Queries.GetSiteById;

public record GetSiteByIdQuery(Guid Id) : IRequest<SiteDto?>;

public class GetSiteByIdQueryHandler : IRequestHandler<GetSiteByIdQuery, SiteDto?>
{
    private readonly IApplicationDbContext _context;

    public GetSiteByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SiteDto?> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var site = await _context.Sites
            .Include(x => x.Setup!)
                .ThenInclude(s => s.SetupDevice)
            .Where(x => x.Id == request.Id && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (site == null)
            return null;

        return new SiteDto
        {
            Id = site.Id,
            UserId = site.UserId,
            Name = site.Name,
            Country = site.Country,
            City = site.City,
            AddressLine = site.AddressLine,
            Latitude = site.Latitude,
            Longitude = site.Longitude,
            Timezone = site.Timezone,
            IsDeleted = site.IsDeleted,
            HasDevice = site.Setup?.SetupDevice != null,
            Created = site.Created.DateTime,
            CreatedBy = site.CreatedBy,
            LastModified = site.LastModified.DateTime,
            LastModifiedBy = site.LastModifiedBy
        };
    }
}
