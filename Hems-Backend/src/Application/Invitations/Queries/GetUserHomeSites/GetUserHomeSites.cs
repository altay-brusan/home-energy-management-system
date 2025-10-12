using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Invitations.Queries.GetUserHomeSites;

public record GetUserHomeSitesQuery : IRequest<UserHomeSitesVm>;

public class GetUserHomeSitesQueryHandler : IRequestHandler<GetUserHomeSitesQuery, UserHomeSitesVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetUserHomeSitesQueryHandler(
        IApplicationDbContext context,
        IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<UserHomeSitesVm> Handle(GetUserHomeSitesQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Kullanıcının sahibi olduğu evler
        var ownedSites = await _context.Sites
            .Where(s => s.UserId == currentUserId && !s.IsDeleted)
            .Select(s => new UserSiteDto
            {
                Id = s.Id,
                UserId = currentUserId,
                SiteId = s.Id,
                SiteName = s.Name,
                Role = UserSiteRole.Owner
            })
            .ToListAsync(cancellationToken);

        // Kullanıcıyla paylaşılan evler
        var sharedSites = await _context.UserSites
            .Where(us => us.UserId == currentUserId && us.Role == UserSiteRole.Guest)
            .Select(us => new UserSiteDto
            {
                Id = us.Id,
                UserId = us.UserId,
                SiteId = us.SiteId,
                SiteName = us.Site.Name,
                Role = us.Role
            })
            .ToListAsync(cancellationToken);

        return new UserHomeSitesVm
        {
            MyHomes = ownedSites,
            SharedHomes = sharedSites
        };
    }
}

