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
    
    // Filter parameters
    public bool? IsToday { get; init; } // Sadece bugünkü bildirimleri getirmek için
    public bool? IsProcessed { get; init; } // Okundu/okunmadı durumuna göre filtreleme
    public string? Category { get; init; } // Notification type'a göre filtreleme (INFO, WARNING, ERROR)
    public bool? IncludeDeleted { get; init; } = false; // Silinen bildirimleri dahil et
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

        var query = _context.DeviceNotifications
            .Include(dn => dn.DeviceNotificationType)
            .Include(dn => dn.Device)
            .Include(dn => dn.Site)
            .Where(dn => dn.SiteId == request.SiteId);

        // IsDeleted filtresi - varsayılan olarak silinen bildirimleri gösterme
        if (request.IncludeDeleted != true)
        {
            query = query.Where(dn => !dn.IsDeleted);
        }

        // IsToday filtresi - sadece bugünkü bildirimleri getir
        if (request.IsToday == true)
        {
            var today = DateTimeOffset.Now.Date;
            var tomorrow = today.AddDays(1);
            query = query.Where(dn => dn.Timestamp >= today && dn.Timestamp < tomorrow);
        }

        // IsProcessed filtresi - okundu/okunmadı durumuna göre filtreleme
        if (request.IsProcessed.HasValue)
        {
            query = query.Where(dn => dn.IsProcessed == request.IsProcessed.Value);
        }

        // Category filtresi - notification type'a göre filtreleme
        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(dn => dn.DeviceNotificationType.Category == request.Category);
        }

        var notifications = await query
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
                IsDeleted = dn.IsDeleted,
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
