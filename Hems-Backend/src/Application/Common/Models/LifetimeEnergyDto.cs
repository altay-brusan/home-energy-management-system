namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Lifetime energy data from the most recent LifetimeEnergy record
/// </summary>
public record LifetimeEnergyDto
{
    /// <summary>
    /// Timestamp of the lifetime energy record
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Lifetime grid import energy in kWh
    /// </summary>
    public double GridImport { get; init; }

    /// <summary>
    /// Lifetime grid export energy in kWh
    /// </summary>
    public double GridExport { get; init; }
}
