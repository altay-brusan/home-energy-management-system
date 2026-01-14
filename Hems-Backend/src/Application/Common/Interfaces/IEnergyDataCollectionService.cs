using Hems_Backend.Application.Common.Models;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for proactively collecting daily energy data from devices via RPC.
/// Handles fallback estimation from power data when devices are unavailable.
/// </summary>
public interface IEnergyDataCollectionService
{
    /// <summary>
    /// Main job entry point - processes all devices that need energy data collection.
    /// Called by Hangfire every 15 minutes.
    /// </summary>
    Task ProcessEnergyDataCollectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a single device - attempts RPC or fallback estimation.
    /// </summary>
    Task ProcessDeviceEnergyDataAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch daily energy data via RPC from device.
    /// </summary>
    /// <param name="thingsBoardDeviceId">The ThingsBoard device ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Daily energy data from device, or null if RPC fails</returns>
    Task<DailyEnergyRpcResult?> FetchDailyEnergyViaRpcAsync(
        Guid thingsBoardDeviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch lifetime energy data via RPC from device.
    /// </summary>
    /// <param name="thingsBoardDeviceId">The ThingsBoard device ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lifetime energy data from device, or null if RPC fails</returns>
    Task<LifetimeEnergyRpcResult?> FetchLifetimeEnergyViaRpcAsync(
        Guid thingsBoardDeviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimate daily energy from power data (fallback when RPC fails).
    /// Uses trapezoidal integration of power readings.
    /// </summary>
    /// <param name="thingsBoardDeviceId">The ThingsBoard device ID</param>
    /// <param name="date">The date to estimate energy for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Estimated daily energy based on power readings</returns>
    Task<EstimatedDailyEnergy> EstimateDailyEnergyFromPowerDataAsync(
        Guid thingsBoardDeviceId,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate lifetime energy by adding daily to previous lifetime.
    /// </summary>
    /// <param name="thingsBoardDeviceId">The ThingsBoard device ID</param>
    /// <param name="dailyEnergy">The daily energy to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New lifetime energy totals</returns>
    Task<CalculatedLifetimeEnergy> CalculateLifetimeEnergyAsync(
        Guid thingsBoardDeviceId,
        EstimatedDailyEnergy dailyEnergy,
        CancellationToken cancellationToken = default);
}
