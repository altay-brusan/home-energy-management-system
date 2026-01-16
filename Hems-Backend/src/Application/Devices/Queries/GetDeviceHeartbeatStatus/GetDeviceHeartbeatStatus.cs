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
    private readonly IDemoSimulationDataService _demoSimulationService;
    private readonly ILogger<GetDeviceHeartbeatStatusQueryHandler> _logger;

    public GetDeviceHeartbeatStatusQueryHandler(
        IDeviceHeartbeatCache heartbeatCache,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService,
        IDemoSimulationDataService demoSimulationService,
        ILogger<GetDeviceHeartbeatStatusQueryHandler> logger)
    {
        _heartbeatCache = heartbeatCache;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
        _demoSimulationService = demoSimulationService;
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

            // Use centralized demo simulation service
            var demoData = _demoSimulationService.GetDemoHeartbeatStatus(request.DeviceId);
            return new DeviceHeartbeatStatusDto
            {
                DeviceId = request.DeviceId,
                Status = demoData.Status,
                Timestamp = demoData.Timestamp
            };
        }

        // Map local device ID to ThingsBoard device ID
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);

        if (thingsBoardDeviceId == null)
        {
            throw new Hems_Backend.Application.Common.Exceptions.NotFoundException("Device", request.DeviceId);
        }

        // Get full state from cache (includes last heartbeat time)
        var state = _heartbeatCache.GetDeviceState(thingsBoardDeviceId.Value);
        var status = state?.Status ?? DeviceStatus.NOT_TRACKED;

        return new DeviceHeartbeatStatusDto
        {
            DeviceId = request.DeviceId,
            Status = status.ToString(),
            Timestamp = DateTime.UtcNow,
            LastHeartbeat = state?.LastHeartbeat,
            LastPowerFlow = state?.LastPowerFlow,
            MissedHeartbeats = state?.MissedHeartbeats ?? 0
        };
    }
}

public class DeviceHeartbeatStatusDto
{
    public Guid DeviceId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public DateTime? LastHeartbeat { get; init; }
    public DateTime? LastPowerFlow { get; init; }
    public int MissedHeartbeats { get; init; }
}
