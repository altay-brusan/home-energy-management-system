using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Hems_Backend.Web.Services;

/// <summary>
/// Service that sends device settings update notifications via SignalR
/// </summary>
public class DeviceSettingsNotificationService : IDeviceSettingsNotificationService
{
    private readonly IHubContext<DeviceSettingsHub, IDeviceSettingsHubClient> _hubContext;

    public DeviceSettingsNotificationService(IHubContext<DeviceSettingsHub, IDeviceSettingsHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyParameterUpdateStartedAsync(ParameterUpdateStartedMessage message, CancellationToken cancellationToken = default)
    {
        var deviceGroup = DeviceSettingsHub.GetDeviceGroupName(message.DeviceId);
        var batchGroup = DeviceSettingsHub.GetBatchGroupName(message.CorrelationId);

        await Task.WhenAll(
            _hubContext.Clients.Group(deviceGroup).ParameterUpdateStarted(message),
            _hubContext.Clients.Group(batchGroup).ParameterUpdateStarted(message)
        );
    }

    public async Task NotifyParameterUpdatedAsync(ParameterUpdatedMessage message, CancellationToken cancellationToken = default)
    {
        var deviceGroup = DeviceSettingsHub.GetDeviceGroupName(message.DeviceId);
        var batchGroup = DeviceSettingsHub.GetBatchGroupName(message.CorrelationId);

        await Task.WhenAll(
            _hubContext.Clients.Group(deviceGroup).ParameterUpdated(message),
            _hubContext.Clients.Group(batchGroup).ParameterUpdated(message)
        );
    }

    public async Task NotifyParameterUpdateFailedAsync(ParameterUpdateFailedMessage message, CancellationToken cancellationToken = default)
    {
        var deviceGroup = DeviceSettingsHub.GetDeviceGroupName(message.DeviceId);
        var batchGroup = DeviceSettingsHub.GetBatchGroupName(message.CorrelationId);

        await Task.WhenAll(
            _hubContext.Clients.Group(deviceGroup).ParameterUpdateFailed(message),
            _hubContext.Clients.Group(batchGroup).ParameterUpdateFailed(message)
        );
    }

    public async Task NotifyParameterUpdateCompletedAsync(ParameterUpdateCompletedMessage message, CancellationToken cancellationToken = default)
    {
        var deviceGroup = DeviceSettingsHub.GetDeviceGroupName(message.DeviceId);
        var batchGroup = DeviceSettingsHub.GetBatchGroupName(message.CorrelationId);

        await Task.WhenAll(
            _hubContext.Clients.Group(deviceGroup).ParameterUpdateCompleted(message),
            _hubContext.Clients.Group(batchGroup).ParameterUpdateCompleted(message)
        );
    }
}
