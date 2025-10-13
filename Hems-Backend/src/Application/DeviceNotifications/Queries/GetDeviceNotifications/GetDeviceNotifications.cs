using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceNotifications.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceNotifications.Queries.GetDeviceNotifications;

public record GetDeviceNotificationsQuery : IRequest<PaginatedList<DeviceNotificationDto>>
{
    public Guid SiteId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetDeviceNotificationsQueryHandler : IRequestHandler<GetDeviceNotificationsQuery, PaginatedList<DeviceNotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetDeviceNotificationsQueryHandler(
        IApplicationDbContext context,
        IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<PaginatedList<DeviceNotificationDto>> Handle(GetDeviceNotificationsQuery request, CancellationToken cancellationToken)
    {
        // Site yetki kontrolü
        await ValidateSiteAccessAsync(request.SiteId, cancellationToken);

        var notifications = await _context.DeviceNotifications
            .Include(dn => dn.DeviceNotificationType)
            .Include(dn => dn.Device)
            .Include(dn => dn.Site)
            .Where(dn => dn.SiteId == request.SiteId)
            .OrderByDescending(dn => dn.Timestamp)
            .Select(dn => new DeviceNotificationDto
            {
                Id = dn.Id,
                DeviceId = dn.DeviceId,
                SiteId = dn.SiteId,
                SetupDeviceId = dn.SetupDeviceId,
                DeviceNotificationTypeId = dn.DeviceNotificationTypeId,
                Timestamp = dn.Timestamp,
                AdditionalData = dn.AdditionalData,
                IsProcessed = dn.IsProcessed,
                Created = dn.Created,
                DeviceNotificationType = new DeviceNotificationTypeDto
                {
                    Id = dn.DeviceNotificationType.Id,
                    Code = dn.DeviceNotificationType.Code,
                    Message = dn.DeviceNotificationType.Message,
                    Status = dn.DeviceNotificationType.Status,
                    Description = dn.DeviceNotificationType.Description,
                    Register = dn.DeviceNotificationType.Register,
                    Value = dn.DeviceNotificationType.Value,
                    Category = dn.DeviceNotificationType.Category,
                    Version = dn.DeviceNotificationType.Version,
                    IsActive = dn.DeviceNotificationType.IsActive
                },
                DeviceManufacturer = dn.Device.Manufacturer,
                SiteName = dn.Site.Name
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize);

        return notifications;
    }

    private async Task ValidateSiteAccessAsync(Guid siteId, CancellationToken cancellationToken)
    {
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
