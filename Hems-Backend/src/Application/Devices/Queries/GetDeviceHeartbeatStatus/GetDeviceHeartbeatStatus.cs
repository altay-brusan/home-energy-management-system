using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<GetDeviceHeartbeatStatusQueryHandler> _logger;

    public GetDeviceHeartbeatStatusQueryHandler(
        IDeviceHeartbeatCache heartbeatCache,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService,
        ILogger<GetDeviceHeartbeatStatusQueryHandler> logger)
    {
        _heartbeatCache = heartbeatCache;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
        _logger = logger;
    }

    public async Task<DeviceHeartbeatStatusDto> Handle(GetDeviceHeartbeatStatusQuery request, CancellationToken cancellationToken)
    {
        // Check if demo device - always return ONLINE status
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);

        _logger.LogInformation(
            "GetDeviceHeartbeatStatus: DeviceId={DeviceId}, IsDemoDevice={IsDemoDevice}",
            request.DeviceId, isDemoDevice);

        if (isDemoDevice)
        {
            _logger.LogInformation(
                "GetDeviceHeartbeatStatus: Returning OK for demo device {DeviceId}",
                request.DeviceId);

            // IMPORTANT: Send "OK" not "ONLINE" - mobile app expects DeviceStatusType enum values
            // DeviceStatusType = 'OK' | 'PENDING' | 'WAITING' | 'DISCONNECTED' | 'NOT_TRACKED'
            return new DeviceHeartbeatStatusDto
            {
                DeviceId = request.DeviceId,
                Status = "OK",
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
