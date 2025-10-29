using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceNotifications.Commands.PermanentDeleteDeviceNotification;

public record PermanentDeleteDeviceNotificationCommand : IRequest
{
    public Guid NotificationId { get; init; }
}

public class PermanentDeleteDeviceNotificationCommandHandler : IRequestHandler<PermanentDeleteDeviceNotificationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public PermanentDeleteDeviceNotificationCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
    }

    public async Task Handle(PermanentDeleteDeviceNotificationCommand request, CancellationToken cancellationToken)
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

        // Hard delete - veritabanından tamamen sil
        _context.DeviceNotifications.Remove(notification);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateNotificationAccessAsync(Guid siteId, CancellationToken cancellationToken)
    {
        // Admin kullanıcılar tüm site'lara erişebilir
        if (_user.Id != null)
        {
            var isAdmin = await _identityService.IsInRoleAsync(_user.Id, "Administrator");
            if (isAdmin)
            {
                return;
            }
        }

        // Site sahibi kontrolü
        var isOwner = await _context.Sites
            .AnyAsync(s => s.Id == siteId && s.UserId == _user.Id, cancellationToken);

        if (isOwner) return;

        // UserSite erişim kontrolü
        var hasUserSiteAccess = await _context.UserSites
            .AnyAsync(us => us.SiteId == siteId && us.UserId == _user.Id, cancellationToken);

        if (!hasUserSiteAccess)
        {
            throw new UnauthorizedAccessException("Notification access denied");
        }
    }
}

