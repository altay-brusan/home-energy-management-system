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
