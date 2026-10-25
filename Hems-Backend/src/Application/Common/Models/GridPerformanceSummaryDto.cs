namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Grid performance summary showing energy bought from and sold to the grid
/// for different time periods (Month, Year, Lifetime)
/// </summary>
public record GridPerformanceSummaryDto
{
    public GridEnergyPeriodDto Month { get; init; } = new();
    public GridEnergyPeriodDto Year { get; init; } = new();
    public GridEnergyPeriodDto Lifetime { get; init; } = new();
}

/// <summary>
/// Grid energy data for a specific period
/// All values in kWh
/// </summary>
public record GridEnergyPeriodDto
{
    /// <summary>
    /// Energy imported from grid (bought) in kWh
    /// </summary>
    public double GridImport { get; init; }

    /// <summary>
    /// Energy exported to grid (sold) in kWh
    /// </summary>
    public double GridExport { get; init; }
}
