using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceNotifications.Commands.DeleteDeviceNotification;

public record DeleteDeviceNotificationCommand : IRequest
{
    public Guid NotificationId { get; init; }
}

public class DeleteDeviceNotificationCommandHandler : IRequestHandler<DeleteDeviceNotificationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public DeleteDeviceNotificationCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
    }

    public async Task Handle(DeleteDeviceNotificationCommand request, CancellationToken cancellationToken)
    {
        // Find notification and verify user access
        var notification = await _context.DeviceNotifications
            .Include(dn => dn.Site)
            .FirstOrDefaultAsync(dn => dn.Id == request.NotificationId, cancellationToken);

        if (notification == null)
        {
            throw AppException.NotFound(
                MessageCodes.NOTIFICATION_NOT_FOUND,
                new Dictionary<string, object> { ["notificationId"] = request.NotificationId });
        }

        // Validate site access
        await ValidateNotificationAccessAsync(notification.SiteId, cancellationToken);

        // Soft delete
        notification.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateNotificationAccessAsync(Guid siteId, CancellationToken cancellationToken)
    {
        // Admin users can access all sites
        if (_user.Id != null)
        {
            var isAdmin = await _identityService.IsInRoleAsync(_user.Id, "Administrator");
            if (isAdmin)
            {
                return;
            }
        }

        // Check if user is site owner
        var isOwner = await _context.Sites
            .AnyAsync(s => s.Id == siteId && s.UserId == _user.Id, cancellationToken);

        if (isOwner) return;

        // Check UserSite access
        var hasUserSiteAccess = await _context.UserSites
            .AnyAsync(us => us.SiteId == siteId && us.UserId == _user.Id, cancellationToken);

        if (!hasUserSiteAccess)
        {
            throw AppException.Forbidden(
                MessageCodes.NOTIFICATION_ACCESS_DENIED,
                new Dictionary<string, object> { ["siteId"] = siteId });
        }
    }
}

