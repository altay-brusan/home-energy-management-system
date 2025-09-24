using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Sites.Queries.GetSitesByUser;

public record GetSitesByUserQuery : IRequest<List<SiteDto>>;

public class GetSitesByUserQueryHandler : IRequestHandler<GetSitesByUserQuery, List<SiteDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetSitesByUserQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<List<SiteDto>> Handle(GetSitesByUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        return await _context.Sites
            .Where(x => x.UserId == userId && !x.IsDeleted)
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
            .ToListAsync(cancellationToken);
    }
}
