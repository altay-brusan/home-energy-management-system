using System.Text.Json;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for managing devices and communications with ThingsBoard platform
/// </summary>
public interface IThingsBoardConnectorService
{
    // ============ DEVICE CRUD OPERATIONS ============
    
    /// <summary>
    /// Creates a new device in ThingsBoard with server attributes
    /// </summary>
    Task<TbDeviceCreateResult> CreateDeviceAsync(
        TbDeviceCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing device in ThingsBoard
    /// </summary>
    Task<bool> UpdateDeviceAsync(
        Guid deviceId,
        TbDeviceUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a device from ThingsBoard
    /// </summary>
    Task<bool> DeleteDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a device by serial number or QR code and returns the ThingsBoard device ID
    /// </summary>
    Task<TbDeviceInfo?> FindDeviceAsync(
        string? serialNumber,
        string? qrCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets device information by ThingsBoard device ID
    /// </summary>
    Task<TbDeviceInfo?> GetDeviceByIdAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets device credentials (access token) from ThingsBoard
    /// </summary>
    Task<TbDeviceCredentials?> GetDeviceCredentialsAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    // ============ ATTRIBUTES OPERATIONS ============

    /// <summary>
    /// Saves server-side attributes for a device
    /// </summary>
    Task<bool> SaveDeviceAttributesAsync(
        Guid deviceId,
        Dictionary<string, object> attributes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server-side attributes for a device
    /// </summary>
    Task<Dictionary<string, object>?> GetDeviceAttributesAsync(
        Guid deviceId,
        string? keys = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes server-side attributes for a device
    /// </summary>
    Task<bool> DeleteDeviceAttributesAsync(
        Guid deviceId,
        string keys,
        CancellationToken cancellationToken = default);

    // ============ TELEMETRY OPERATIONS ============

    /// <summary>
    /// Saves telemetry data for a device
    /// </summary>
    Task<bool> SaveDeviceTelemetryAsync(
        Guid deviceId,
        Dictionary<string, object> telemetry,
        long? timestamp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets latest telemetry for a device
    /// </summary>
    Task<Dictionary<string, TbTelemetryValue>?> GetLatestTelemetryAsync(
        Guid deviceId,
        string? keys = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets time-series telemetry data for a device
    /// </summary>
    Task<Dictionary<string, List<TbTelemetryValue>>?> GetTimeSeriesTelemetryAsync(
        Guid deviceId,
        string keys,
        long startTs,
        long endTs,
        int limit = 100,
        CancellationToken cancellationToken = default);

    // ============ RPC OPERATIONS ============

    /// <summary>
    /// Sends a one-way RPC command to a device
    /// </summary>
    Task<bool> SendOneWayRpcAsync(
        Guid deviceId,
        string method,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a two-way RPC command to a device and waits for response.
    /// When parameters exceed batchSize, they are sent in batches to avoid timeouts.
    /// </summary>
    Task<TbRpcResponse?> SendTwoWayRpcAsync(
        Guid deviceId,
        string method,
        Dictionary<string, object> parameters,
        int timeoutMs = 5000,
        int batchSize = 10,
        CancellationToken cancellationToken = default);
}

// ============ DTOs ============

public sealed record TbDeviceCreateRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Label { get; init; }
    public string DeviceProfileName { get; init; } = "default";
    
    // Server attributes to be set on device creation
    public Dictionary<string, object> ServerAttributes { get; init; } = new();
}

public sealed record TbDeviceCreateResult
{
    public Guid DeviceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Label { get; init; }
    public string DeviceProfileName { get; init; } = string.Empty;
}

public sealed record TbDeviceUpdateRequest
{
    public string? Name { get; init; }
    public string? Label { get; init; }
}

public sealed record TbDeviceInfo
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Label { get; init; }
    public string? Type { get; init; }
    public string? DeviceProfileName { get; init; }
}

public sealed record TbTelemetryValue
{
    public long Ts { get; init; } // Timestamp in milliseconds
    public object Value { get; init; } = null!;
}

public sealed record TbDeviceCredentials
{
    public string AccessToken { get; init; } = string.Empty;
    public string CredentialsType { get; init; } = string.Empty;
}

public sealed record TbRpcResponse
{
    public bool Success { get; init; }
    public Dictionary<string, object>? Response { get; init; }
    public string? Error { get; init; }
}
