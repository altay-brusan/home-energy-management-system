namespace Hems_Backend.Application.Common.Interfaces;

public interface IAlmService
{
    /// <summary>
    /// Submits a prediction job to the ALM service
    /// </summary>
    /// <param name="csvData">Power data in CSV format (all columns)</param>
    /// <param name="schema">Schema describing the data structure</param>
    /// <param name="callbackUrl">URL for ALM to callback with results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ALM job ID and initial status</returns>
    Task<AlmJobResponse> SubmitPredictionJobAsync(
        string csvData,
        AlmSchema schema,
        string callbackUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status/result of a prediction job
    /// </summary>
    Task<AlmJobStatusResponse?> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);

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

public class AlmJobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public object? Result { get; set; }
    public string? Error { get; set; }
}
