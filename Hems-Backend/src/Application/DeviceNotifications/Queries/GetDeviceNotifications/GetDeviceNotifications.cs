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
    public string? Category { get; init; } // Device category filtreleme (inverter, battery, etc.)
    public string? Severity { get; init; } // Severity filtreleme (WARNING, ERROR, ALARM)
    public bool? IncludeDeleted { get; init; } = false; // Silinen bildirimleri dahil et
}

public class GetDeviceNotificationsQueryHandler : IRequestHandler<GetDeviceNotificationsQuery, PaginatedList<DeviceNotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;
    private readonly ITimezoneService _timezoneService;

    public GetDeviceNotificationsQueryHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService,
        ITimezoneService timezoneService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
        _timezoneService = timezoneService;
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

        // IsToday filtresi - sadece bugünkü bildirimleri getir (site timezone'una göre)
        if (request.IsToday == true)
        {
            // Get site timezone to determine "today" in user's local time
            var siteTimezone = await _timezoneService.GetSiteTimezoneAsync(request.SiteId, cancellationToken);
            var nowInSiteTimezone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, siteTimezone);
            var todayStartLocal = nowInSiteTimezone.Date;
            var tomorrowStartLocal = todayStartLocal.AddDays(1);

            // Convert local time boundaries to UTC for database comparison
            var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayStartLocal, siteTimezone);
            var tomorrowStartUtc = TimeZoneInfo.ConvertTimeToUtc(tomorrowStartLocal, siteTimezone);

            query = query.Where(dn => dn.Timestamp >= todayStartUtc && dn.Timestamp < tomorrowStartUtc);
        }

        // IsProcessed filtresi - okundu/okunmadı durumuna göre filtreleme
        if (request.IsProcessed.HasValue)
        {
            query = query.Where(dn => dn.IsProcessed == request.IsProcessed.Value);
        }

        // Category filtresi - device category'ye göre filtreleme (inverter, battery, etc.)
        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(dn => dn.DeviceNotificationType.Category == request.Category);
        }

        // Severity filtresi - severity'ye göre filtreleme (WARNING, ERROR, ALARM)
        if (!string.IsNullOrEmpty(request.Severity))
        {
            query = query.Where(dn => dn.DeviceNotificationType.Severity == request.Severity);
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
                    Severity = dn.DeviceNotificationType.Severity,
                    Version = dn.DeviceNotificationType.Version,
                    Manufacturer = dn.DeviceNotificationType.Manufacturer,
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
            throw new UnauthorizedAccessException($"You don't have access to site {siteId}");
        }
    }
}
