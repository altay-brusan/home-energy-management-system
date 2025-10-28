using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceNotifications.Commands.MarkAllSiteNotificationsAsRead;

public record MarkAllSiteNotificationsAsReadCommand : IRequest<int>
{
    public Guid SiteId { get; init; }
}

public class MarkAllSiteNotificationsAsReadCommandHandler : IRequestHandler<MarkAllSiteNotificationsAsReadCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public MarkAllSiteNotificationsAsReadCommandHandler(
        IApplicationDbContext context,
        IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(MarkAllSiteNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        // Site erişim yetkisini kontrol et
        await ValidateSiteAccessAsync(request.SiteId, cancellationToken);

        // Site'e ait tüm okunmamış notification'ları bul
        var unreadNotifications = await _context.DeviceNotifications
            .Where(dn => dn.SiteId == request.SiteId 
                      && !dn.IsProcessed 
                      && !dn.IsDeleted)
            .ToListAsync(cancellationToken);

        if (!unreadNotifications.Any())
        {
            return 0;
        }

        // Tüm notification'ları okundu olarak işaretle
        foreach (var notification in unreadNotifications)
        {
            notification.IsProcessed = true;
        }

        // Değişiklikleri kaydet
        var affectedRows = await _context.SaveChangesAsync(cancellationToken);

        return affectedRows;
    }

    private async Task ValidateSiteAccessAsync(Guid siteId, CancellationToken cancellationToken)
    {
        //TODO: Admin Kontrolü.
        var isOwner = await _context.Sites
            .AnyAsync(s => s.Id == siteId && s.UserId == _user.Id, cancellationToken);

        if (isOwner) return;

        var hasUserSiteAccess = await _context.UserSites
            .AnyAsync(us => us.SiteId == siteId && us.UserId == _user.Id, cancellationToken);

        if (!hasUserSiteAccess)
        {
            throw new UnauthorizedAccessException($"You don't have access to site {siteId}");
        }
    }
}
