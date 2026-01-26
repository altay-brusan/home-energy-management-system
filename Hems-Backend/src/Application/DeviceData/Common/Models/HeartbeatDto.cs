namespace Hems_Backend.Application.DeviceData.Common.Models;

/// <summary>
/// DTO for device heartbeat telemetry
/// </summary>
public record HeartbeatDto
{
    /// <summary>
    /// ThingsBoard Device ID (GUID format)
    /// </summary>
    public Guid DeviceId { get; init; }

    /// <summary>
    /// Timestamp from device (for NTP sync validation)
    /// </summary>
    public DateTime Timestamp { get; init; }
}
