using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.Devices.Queries.GetDeviceHeartbeatStatus;

public record GetDeviceHeartbeatStatusQuery : IRequest<DeviceHeartbeatStatusDto>
{
    public Guid DeviceId { get; init; }
}

public class GetDeviceHeartbeatStatusQueryHandler : IRequestHandler<GetDeviceHeartbeatStatusQuery, DeviceHeartbeatStatusDto>
{
    private readonly IDeviceHeartbeatCache _heartbeatCache;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDemoDataService _demoDataService;

    public GetDeviceHeartbeatStatusQueryHandler(
        IDeviceHeartbeatCache heartbeatCache,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService)
    {
        _heartbeatCache = heartbeatCache;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
    }

    public async Task<DeviceHeartbeatStatusDto> Handle(GetDeviceHeartbeatStatusQuery request, CancellationToken cancellationToken)
    {
        // Check if demo device - always return ONLINE status
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return new DeviceHeartbeatStatusDto
            {
                DeviceId = request.DeviceId,
                Status = "ONLINE",
                Timestamp = DateTime.UtcNow
            };
        }

        // Map local device ID to ThingsBoard device ID
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);

        if (thingsBoardDeviceId == null)
        {
            throw new Hems_Backend.Application.Common.Exceptions.NotFoundException("Device", request.DeviceId);
        }

        // Get current status from cache
        var status = _heartbeatCache.GetDeviceStatus(thingsBoardDeviceId.Value);

        return new DeviceHeartbeatStatusDto
        {
            DeviceId = request.DeviceId,
            Status = status.ToString(),
            Timestamp = DateTime.UtcNow
        };
    }
}

public class DeviceHeartbeatStatusDto
{
    public Guid DeviceId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
