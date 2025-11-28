using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;

public record CheckDeviceAccessQuery : IRequest<bool>
{
    public Guid DeviceId { get; init; }
}

public class CheckDeviceAccessQueryHandler : IRequestHandler<CheckDeviceAccessQuery, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public CheckDeviceAccessQueryHandler(IApplicationDbContext context, IUser user, IIdentityService identityService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
    }

    public async Task<bool> Handle(CheckDeviceAccessQuery request, CancellationToken cancellationToken)
    {
        // User ID null kontrolü
        if (_user.Id == null)
        {
            return false;
        }

        // Admin kullanıcılar tüm device'lara erişebilir
        var isAdmin = await _identityService.IsInRoleAsync(_user.Id, "Administrator");
        if (isAdmin)
        {
            return true;
        }

        // Normal kullanıcılar sadece kendi device'larına veya paylaşılan device'lara erişebilir
        // DeviceId is actually ThingsBoardDeviceId, so we need to check against Device.ThingsBoardDeviceId
        var hasAccess = await _context.SetupDevices
            .AnyAsync(sd => _context.Devices.Any(d => d.Id == sd.DeviceId && d.ThingsBoardDeviceId == request.DeviceId) && 
                           _context.Setups.Any(s => s.Id == sd.SetupId && 
                                                  _context.Sites.Any(site => site.Id == s.SiteId && 
                                                                           (site.CreatedBy == _user.Id || 
                                                                            _context.UserSites.Any(us => us.SiteId == site.Id && us.UserId == _user.Id)))), 
                      cancellationToken);

        return hasAccess;
    }
}
