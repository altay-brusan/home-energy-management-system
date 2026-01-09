using Hems_Backend.Application.PowerFlow.Common.Models;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for fetching PowerFlow data from devices.
/// Supports both real devices (via ThingsBoard RPC) and demo devices (simulated data).
/// </summary>
public interface IPowerFlowService
{
    /// <summary>
    /// Calls ThingsBoard (via RPC) and returns the latest PowerFlow data for a device.
    /// For demo devices, returns simulated data instead.
    /// </summary>
    /// <param name="deviceId">The device ID to fetch PowerFlow for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PowerFlowDto with current power values, or null if fetch failed</returns>
    Task<PowerFlowDto?> GetPowerFlowAsync(Guid deviceId, CancellationToken cancellationToken = default);
}
