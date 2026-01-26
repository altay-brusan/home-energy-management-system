using Hems_Backend.Application.Common.Models;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Strongly-typed client interface for DeviceSettingsHub.
/// Defines the methods that can be called on connected clients.
/// </summary>
public interface IDeviceSettingsHubClient
{
    /// <summary>
    /// Called when a parameter update operation starts.
    /// </summary>
    Task OperationStarted(OperationStartedEvent evt);

    /// <summary>
    /// Called after each parameter is processed (success or failure).
    /// </summary>
    Task ParameterProgress(ParameterProgressEvent evt);

    /// <summary>
    /// Called when the entire operation completes.
    /// </summary>
    Task OperationCompleted(OperationCompletedEvent evt);
}
