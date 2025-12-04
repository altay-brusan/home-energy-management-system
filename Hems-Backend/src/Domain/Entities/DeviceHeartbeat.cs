namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Entity for tracking device heartbeat attempts and status
/// </summary>
public class DeviceHeartbeat
{
    public Guid Id { get; set; }
    public Guid ThingsBoardDeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public HeartbeatStatus Status { get; set; }
    public int AttemptNumber { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime? ResponseReceivedAt { get; set; }
}

/// <summary>
/// Status of heartbeat attempt
/// </summary>
public enum HeartbeatStatus
{
    Sent = 1,
    ResponseReceived = 2,
    NoResponse = 3,
    DeviceOffline = 4
}
