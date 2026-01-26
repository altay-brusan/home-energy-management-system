namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Response DTO for energy source mix (pie chart data)
/// Shows how much of the total load energy came from each source
/// Energy values are in Wh (Watt-hours)
/// </summary>
public record EnergySourceMixDto
{
    /// <summary>
    /// The date for which the distribution is calculated (in device local time)
    /// Format: yyyy-MM-dd
    /// </summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// The device ID
    /// </summary>
    public Guid DeviceId { get; init; }

    /// <summary>
    /// Total load energy demand in Wh
    /// </summary>
    public double LoadDemandWh { get; init; }

    /// <summary>
    /// List of energy sources with their contributions to the load
    /// </summary>
    public List<EnergySourceContribution> Sources { get; init; } = new();

    /// <summary>
    /// Summary statistics
    /// </summary>
    public EnergySourceMixSummary Summary { get; init; } = new();
}

/// <summary>
/// Represents a single energy source contribution to the load
/// </summary>
public record EnergySourceContribution
{
    /// <summary>
    /// Source name: "Solar", "Battery", "Grid", "Generator"
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Energy provided to load in Wh
    /// </summary>
    public double ProvidedEnergyWh { get; init; }

    /// <summary>
    /// Percentage contribution to total load (0-100)
    /// </summary>
    public double Contribution { get; init; }
}

/// <summary>
/// Summary statistics for energy source mix
/// </summary>
public record EnergySourceMixSummary
{
    /// <summary>
    /// Total number of power samples processed
    /// </summary>
    public int SampleCount { get; init; }

    /// <summary>
    /// First sample timestamp (HH:mm format)
    /// </summary>
    public string? FirstSampleTime { get; init; }

    /// <summary>
    /// Last sample timestamp (HH:mm format)
    /// </summary>
    public string? LastSampleTime { get; init; }

    /// <summary>
    /// Average interval between samples in minutes
    /// </summary>
    public double AverageIntervalMinutes { get; init; }

    /// <summary>
    /// Total time span covered in minutes
    /// </summary>
    public double TotalTimeSpanMinutes { get; init; }

    /// <summary>
    /// Number of samples where load existed but no supply was detected (edge case)
    /// </summary>
    public int ZeroSupplySamples { get; init; }
}
