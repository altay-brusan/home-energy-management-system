namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Result from getDailyEnergyData RPC call
/// </summary>
public record DailyEnergyRpcResult
{
    public double? PvTotalEnergyKWh { get; init; }
    public double? GridImportEnergyKWh { get; init; }
    public double? GridExportEnergyKWh { get; init; }
    public double? BatteryChargeEnergyKWh { get; init; }
    public double? BatteryDischargeEnergyKWh { get; init; }
    public double? LoadEnergyKWh { get; init; }
    public double? GeneratorEnergyKWh { get; init; }

    // MPPT individual values (optional)
    public double? PvMppt1EnergyKWh { get; init; }
    public double? PvMppt2EnergyKWh { get; init; }
    public double? PvMppt3EnergyKWh { get; init; }
    public double? PvMppt4EnergyKWh { get; init; }
}

/// <summary>
/// Result from getLifetimeEnergyData RPC call
/// </summary>
public record LifetimeEnergyRpcResult
{
    public double? PvTotalEnergyKWh { get; init; }
    public double? GridImportEnergyKWh { get; init; }
    public double? GridExportEnergyKWh { get; init; }
    public double? BatteryChargeEnergyKWh { get; init; }
    public double? BatteryDischargeEnergyKWh { get; init; }
    public double? LoadEnergyKWh { get; init; }
    public double? GeneratorEnergyKWh { get; init; }

    // MPPT individual values (optional)
    public double? PvMppt1EnergyKWh { get; init; }
    public double? PvMppt2EnergyKWh { get; init; }
    public double? PvMppt3EnergyKWh { get; init; }
    public double? PvMppt4EnergyKWh { get; init; }
}

/// <summary>
/// Estimated daily energy calculated from power data.
/// Used as fallback when device is unavailable.
/// </summary>
public record EstimatedDailyEnergy
{
    public double PvTotalEnergyKWh { get; init; }
    public double GridImportEnergyKWh { get; init; }
    public double GridExportEnergyKWh { get; init; }
    public double BatteryChargeEnergyKWh { get; init; }
    public double BatteryDischargeEnergyKWh { get; init; }
    public double LoadEnergyKWh { get; init; }
    public double GeneratorEnergyKWh { get; init; }

    /// <summary>
    /// Indicates this data was estimated from power readings, not received from device.
    /// </summary>
    public bool IsEstimated { get; init; } = true;

    /// <summary>
    /// Number of power data points used for estimation.
    /// Useful for quality assessment - more points = more accurate.
    /// </summary>
    public int PowerDataPointsUsed { get; init; }
}

/// <summary>
/// Calculated lifetime energy data (cumulative since device installation).
/// </summary>
public record CalculatedLifetimeEnergy
{
    public double PvTotalEnergyKWh { get; init; }
    public double GridImportEnergyKWh { get; init; }
    public double GridExportEnergyKWh { get; init; }
    public double BatteryChargeEnergyKWh { get; init; }
    public double BatteryDischargeEnergyKWh { get; init; }
    public double LoadEnergyKWh { get; init; }
    public double GeneratorEnergyKWh { get; init; }
}

/// <summary>
/// Tracking record for energy collection attempts.
/// Used to avoid duplicate processing within the same collection window.
/// </summary>
public record EnergyCollectionStatus
{
    public Guid DeviceId { get; init; }
    public DateTime Date { get; init; }
    public bool DailyRpcAttempted { get; init; }
    public bool DailyRpcSucceeded { get; init; }
    public bool LifetimeRpcAttempted { get; init; }
    public bool LifetimeRpcSucceeded { get; init; }
    public int RetryCount { get; init; }
    public DateTime? LastAttemptUtc { get; init; }
}
