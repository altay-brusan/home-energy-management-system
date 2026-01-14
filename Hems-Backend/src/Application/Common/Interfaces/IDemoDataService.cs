using Hems_Backend.Application.PowerFlow.Common.Models;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for managing demo user data.
/// Handles data reset on login and provides demo-specific operations.
/// </summary>
public interface IDemoDataService
{
    /// <summary>
    /// Checks if a user is a demo user by their user ID.
    /// </summary>
    Task<bool> IsDemoUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user is a demo user by their email.
    /// </summary>
    Task<bool> IsDemoUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a device is a demo device.
    /// </summary>
    Task<bool> IsDemoDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all demo data for a demo user.
    /// This clears telemetry data, notifications, and restores default settings.
    /// Called on each demo user login to provide a fresh demo environment.
    /// </summary>
    Task<DemoDataResetResult> ResetDemoDataAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds initial demo data for a demo user.
    /// Creates demo site, setup, device, and components.
    /// </summary>
    Task<DemoDataSeedResult> SeedDemoDataAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the well-known demo user email.
    /// </summary>
    string GetDemoUserEmail();

    /// <summary>
    /// Gets the well-known demo device serial number.
    /// </summary>
    string GetDemoDeviceSerialNumber();
}

/// <summary>
/// Result of demo data reset operation.
/// </summary>
public record DemoDataResetResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int TelemetryRecordsDeleted { get; init; }
    public int NotificationsDeleted { get; init; }
    public int EnergyDataRecordsDeleted { get; init; }
}

/// <summary>
/// Result of demo data seeding operation.
/// </summary>
public record DemoDataSeedResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? SiteId { get; init; }
    public Guid? SetupId { get; init; }
    public Guid? DeviceId { get; init; }
}

/// <summary>
/// Service for generating simulated data for demo users.
/// Provides realistic power flow, energy, and weather data based on time of day.
/// Also manages in-memory storage for demo device settings (prices, etc.)
/// </summary>
public interface IDemoSimulationDataService
{
    /// <summary>
    /// Gets simulated PowerFlow data for the current moment
    /// </summary>
    PowerFlowDto GetCurrentPowerFlow(Guid deviceId);

    /// <summary>
    /// Gets simulated PowerFlow data for a specific time
    /// </summary>
    PowerFlowDto GetPowerFlowAtTime(Guid deviceId, DateTime timestamp);

    /// <summary>
    /// Gets simulated daily energy data for a specific date
    /// </summary>
    DailyEnergyData GetDailyEnergy(DateTime date);

    /// <summary>
    /// Gets battery state of charge percentage
    /// </summary>
    double GetBatterySoc(DateTime timestamp);

    /// <summary>
    /// Sets energy prices for demo device (stored in-memory)
    /// </summary>
    void SetDemoPrices(Guid deviceId, List<DemoPriceEntry> prices, string unit = "kWh", string currency = "EUR");

    /// <summary>
    /// Gets energy prices for demo device.
    /// Returns user-set prices if available, otherwise returns default Netherlands market prices.
    /// </summary>
    DemoPriceSettings GetDemoPrices(Guid deviceId);

    /// <summary>
    /// Checks if demo device has user-set prices
    /// </summary>
    bool HasCustomPrices(Guid deviceId);

    /// <summary>
    /// Clears custom prices for demo device (reverts to defaults)
    /// </summary>
    void ClearDemoPrices(Guid deviceId);

    /// <summary>
    /// Gets the buy/sell price for a specific hour from demo prices
    /// </summary>
    (decimal SellPrice, decimal BuyPrice) GetPriceForHour(Guid deviceId, int hour);

    /// <summary>
    /// Gets weighted average daily price from demo prices
    /// </summary>
    (decimal SellPrice, decimal BuyPrice) GetWeightedDailyAveragePrice(Guid deviceId);
}

/// <summary>
/// Price entry for demo device (in-memory storage)
/// </summary>
public record DemoPriceEntry
{
    public decimal SellPrice { get; init; }
    public decimal BuyPrice { get; init; }
    public int? BeginHour { get; init; }
    public int? EndHour { get; init; }
}

/// <summary>
/// Price settings for demo device
/// </summary>
public record DemoPriceSettings
{
    public string Unit { get; init; } = "kWh";
    public string Currency { get; init; } = "EUR";
    public List<DemoPriceEntry> Prices { get; init; } = new();
    public bool IsCustom { get; init; }
    public DateTimeOffset Created { get; init; }
}

/// <summary>
/// Simulated daily energy data for demo devices.
/// </summary>
public record DailyEnergyData
{
    public DateTime Date { get; init; }
    public double PvEnergyKWh { get; init; }
    public double LoadEnergyKWh { get; init; }
    public double GridImportEnergyKWh { get; init; }
    public double GridExportEnergyKWh { get; init; }
    public double BatteryChargeEnergyKWh { get; init; }
    public double BatteryDischargeEnergyKWh { get; init; }
    public double GeneratorEnergyKWh { get; init; }
}
