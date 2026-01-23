namespace Hems_Backend.Application.Common.Models;

public record SocHistoryResponseDto
{
    /// <summary>
    /// The date for which the SOC history is returned (in device local time)
    /// </summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// The device ID
    /// </summary>
    public Guid DeviceId { get; init; }

    /// <summary>
    /// List of SOC data points throughout the day
    /// </summary>
    public List<SocHistoryDataPoint> Data { get; init; } = new();
}

public record SocHistoryDataPoint
{
    /// <summary>
    /// Timestamp in HH:mm format (device local time)
    /// </summary>
    public string Timestamp { get; init; } = string.Empty;

    /// <summary>
    /// Battery state of charge percentage (0-100)
    /// </summary>
    public double SocPercent { get; init; }

    /// <summary>
    /// Whether this data point was interpolated (estimated due to missing data)
    /// </summary>
    public bool IsInterpolated { get; init; }
}
