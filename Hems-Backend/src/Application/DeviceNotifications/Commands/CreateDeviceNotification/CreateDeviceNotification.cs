using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceNotifications.Common.Models;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceNotifications.Commands.CreateDeviceNotification;

public record CreateDeviceNotificationCommand : IRequest<DeviceNotificationDto>
{
    public Guid DeviceId { get; init; }
    public string NotificationTypeCode { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string AdditionalData { get; init; } = string.Empty;
}

public class CreateDeviceNotificationCommandHandler : IRequestHandler<CreateDeviceNotificationCommand, DeviceNotificationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateDeviceNotificationCommandHandler(
        IApplicationDbContext context,
        IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<DeviceNotificationDto> Handle(CreateDeviceNotificationCommand request, CancellationToken cancellationToken)
    {
        // Get device with related information
        var device = await _context.Devices
            .Include(d => d.SetupDevice!)
            .ThenInclude(sd => sd.Setup!)
            .ThenInclude(s => s.Site!)
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken);

        if (device == null)
            throw AppException.NotFound(
                MessageCodes.DEVICE_NOT_FOUND,
                new Dictionary<string, object> { ["deviceId"] = request.DeviceId });

        if (device.SetupDevice?.Setup?.Site == null)
            throw AppException.BadRequest(
                MessageCodes.NOTIFICATION_DEVICE_NOT_ASSIGNED,
                new Dictionary<string, object> { ["deviceId"] = request.DeviceId });

        // Get notification type
        var notificationType = await _context.DeviceNotificationTypes
            .FirstOrDefaultAsync(nt => nt.Code == request.NotificationTypeCode, cancellationToken);

        if (notificationType == null)
            throw AppException.NotFound(
                MessageCodes.NOTIFICATION_TYPE_NOT_FOUND,
                new Dictionary<string, object> { ["code"] = request.NotificationTypeCode });

        // Device notification oluştur
        var deviceNotification = new DeviceNotification
        {
            DeviceId = request.DeviceId,
            SiteId = device.SetupDevice.Setup.SiteId,
            SetupDeviceId = device.SetupDevice.Id,
            DeviceNotificationTypeId = notificationType.Id,
            Timestamp = request.Timestamp,
            AdditionalData = request.AdditionalData
        };

        _context.DeviceNotifications.Add(deviceNotification);
        await _context.SaveChangesAsync(cancellationToken);

        // DTO'ya map et
        var result = await _context.DeviceNotifications
            .Include(dn => dn.DeviceNotificationType)
            .Include(dn => dn.Device)
            .Include(dn => dn.Site)
            .Where(dn => dn.Id == deviceNotification.Id)
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
                    Description = dn.DeviceNotificationType.Description,
                    Category = dn.DeviceNotificationType.Category,
                    Severity = dn.DeviceNotificationType.Severity,
                    Version = dn.DeviceNotificationType.Version,
                    Manufacturer = dn.DeviceNotificationType.Manufacturer,
                    IsActive = dn.DeviceNotificationType.IsActive
                },
                DeviceManufacturer = dn.Device.Manufacturer,
                SiteName = dn.Site.Name
            })
            .FirstAsync(cancellationToken);

        return result;
    }
}
