namespace Hems_Backend.Application.Common.Interfaces;

public interface IAlmService
{
    /// <summary>
    /// Submits a prediction job to the ALM service
    /// </summary>
    /// <param name="csvData">Power data in CSV format (all columns)</param>
    /// <param name="schema">Schema describing the data structure</param>
    /// <param name="callbackUrl">URL for ALM to callback with results</param>
    /// <param name="moduleEndpoint">AI container endpoint path (e.g., "/module-tsf-pc")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ALM job ID and initial status</returns>
    Task<AlmJobResponse> SubmitPredictionJobAsync(
        string csvData,
        AlmSchema schema,
        string callbackUrl,
        string moduleEndpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full result of a prediction job from the AI container
    /// </summary>
    /// <param name="moduleEndpoint">AI container endpoint path (e.g., "/module-tsf-pc")</param>
    /// <param name="jobId">The ALM job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Full job result including forecast data, or null if not found</returns>
    Task<AlmJobResultResponse?> GetJobResultAsync(
        string moduleEndpoint,
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the ALM service is healthy
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

public class AlmSchema
{
    public string Id { get; set; } = string.Empty;
    public string TimestampCol { get; set; } = "DeviceLocalTime";
    public string TimestampFormat { get; set; } = "%Y-%m-%d %H:%M:%S.%f";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<string> Features { get; set; } = new();
    public Dictionary<string, string> FeatureMapping { get; set; } = new();
}

public class AlmJobResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Position { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Full job result response from the AI container
/// </summary>
public class AlmJobResultResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public AlmForecastResult? Result { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// Forecast result containing prediction data and metadata
/// </summary>
public class AlmForecastResult
{
    public List<AlmForecastPoint> Forecast { get; set; } = new();
    public AlmForecastMetadata? Metadata { get; set; }
    public string? StatusCode { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Individual forecast data point
/// </summary>
public class AlmForecastPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public int Step { get; set; }
}

/// <summary>
/// Metadata about the forecast
/// </summary>
public class AlmForecastMetadata
{
    public string Feature { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; }
    public int Horizon { get; set; }
    public string Unit { get; set; } = string.Empty;
}
