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
        return await _context.Sites
            .Where(x => x.Id == request.Id && !x.IsDeleted)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
