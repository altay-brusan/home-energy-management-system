using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceNotifications.Commands.RestoreDeviceNotification;

public record RestoreDeviceNotificationCommand : IRequest
{
    public Guid NotificationId { get; init; }
}

public class RestoreDeviceNotificationCommandHandler : IRequestHandler<RestoreDeviceNotificationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RestoreDeviceNotificationCommandHandler(
        IApplicationDbContext context,
        IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(RestoreDeviceNotificationCommand request, CancellationToken cancellationToken)
    {
        // Notification'ı bul ve kullanıcının erişim yetkisini kontrol et
        var notification = await _context.DeviceNotifications
            .Include(dn => dn.Site)
            .FirstOrDefaultAsync(dn => dn.Id == request.NotificationId, cancellationToken);

        if (notification == null)
        {
            throw new ArgumentException($"Notification with ID {request.NotificationId} not found.");
        }

        // Site erişim yetkisini kontrol et
        await ValidateNotificationAccessAsync(notification.SiteId, cancellationToken);

        // Restore - IsDeleted flag'ini false yap
        notification.IsDeleted = false;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateNotificationAccessAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var isOwner = await _context.Sites
            .AnyAsync(s => s.Id == siteId && s.UserId == _user.Id, cancellationToken);

        if (isOwner) return;

        var hasUserSiteAccess = await _context.UserSites
            .AnyAsync(us => us.SiteId == siteId && us.UserId == _user.Id, cancellationToken);

        if (!hasUserSiteAccess)
        {
            throw new UnauthorizedAccessException("Notification access denied");
        }
    }
}

