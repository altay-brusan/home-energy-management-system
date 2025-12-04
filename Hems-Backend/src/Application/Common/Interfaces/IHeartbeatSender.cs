namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for sending heartbeat requests to devices
/// </summary>
public interface IHeartbeatSender
{
    /// <summary>
    /// Send heartbeat request to a device
    /// </summary>
    /// <param name="thingsBoardDeviceId">ThingsBoard device ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if device responded, false otherwise</returns>
    Task<bool> SendHeartbeatAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default);
}
