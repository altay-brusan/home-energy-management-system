using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Sites.Queries.CheckSiteAccess;

public record CheckSiteAccessQuery : IRequest<bool>
{
    public Guid SiteId { get; init; }
}

public class CheckSiteAccessQueryHandler : IRequestHandler<CheckSiteAccessQuery, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public CheckSiteAccessQueryHandler(IApplicationDbContext context, IUser user, IIdentityService identityService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
    }

    public async Task<bool> Handle(CheckSiteAccessQuery request, CancellationToken cancellationToken)
    {
        // User ID null kontrolü
        if (_user.Id == null)
        {
            return false;
        }

        // Admin kullanıcılar tüm site'lara erişebilir
        var isAdmin = await _identityService.IsInRoleAsync(_user.Id, "Administrator");
        if (isAdmin)
        {
            return true;
        }

        // Normal kullanıcılar sadece kendi site'larına veya paylaşılan site'lara erişebilir
        var hasAccess = await _context.Sites
            .AnyAsync(site => site.Id == request.SiteId && 
                             (site.CreatedBy == _user.Id || 
                              _context.UserSites.Any(us => us.SiteId == site.Id && us.UserId == _user.Id)), 
                      cancellationToken);

        return hasAccess;
    }
}
