using Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Interface for sending alarm notifications via SignalR
/// Implemented in Infrastructure layer to keep Web/Infrastructure concerns out of Application layer
/// </summary>
public interface IAlarmNotificationSender
{
    /// <summary>
    /// Sends SignalR notifications for created device notifications
    /// </summary>
    /// <param name="createdNotificationIds">IDs of newly created notifications</param>
    /// <param name="processedNotifications">Processed notification info with display data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendNotificationsAsync(
        List<Guid> createdNotificationIds,
        List<ProcessedNotificationInfo> processedNotifications,
        CancellationToken cancellationToken = default);
}
