namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Response DTO for historical grid performance data
/// Shows grid import/export over different time periods
/// </summary>
public record GridPerformanceResponseDto
{
    /// <summary>
    /// The period type: "monthly", "yearly", or "custom"
    /// </summary>
    public string Period { get; init; } = string.Empty;

    /// <summary>
    /// List of data points for the specified period
    /// </summary>
    public List<GridPerformanceDataPoint> Data { get; init; } = new();

    /// <summary>
    /// Total aggregated values for the entire period
    /// </summary>
    public GridPerformanceTotal Total { get; init; } = new();
}

/// <summary>
/// Individual data point for grid performance
/// All energy values in kWh
/// </summary>
public record GridPerformanceDataPoint
{
    /// <summary>
    /// Timestamp label (e.g., "2024-12-01" for daily, "2024-12" for monthly)
    /// </summary>
    public string Timestamp { get; init; } = string.Empty;

    /// <summary>
    /// Energy imported from grid (bought) in kWh
    /// </summary>
    public double GridImport { get; init; }

    /// <summary>
    /// Energy exported to grid (sold) in kWh
    /// </summary>
    public double GridExport { get; init; }
}

/// <summary>
/// Total aggregated values for grid performance
/// All energy values in kWh
/// </summary>
public record GridPerformanceTotal
{
    /// <summary>
    /// Total energy imported from grid (bought) in kWh
    /// </summary>
    public double TotalGridImport { get; init; }

    /// <summary>
    /// Total energy exported to grid (sold) in kWh
    /// </summary>
    public double TotalGridExport { get; init; }
}
