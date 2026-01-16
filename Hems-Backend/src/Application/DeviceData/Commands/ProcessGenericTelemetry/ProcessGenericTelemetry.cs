using System.Globalization;
using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceData.Commands.ProcessGenericTelemetry;

/// <summary>
/// Result of processing generic telemetry
/// </summary>
public record ProcessGenericTelemetryResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? CreatedId { get; init; }
    public Guid DeviceId { get; init; }
    public DateTime DataDateTime { get; init; }
    public bool IsDuplicate { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Command to process generic telemetry from ThingsBoard (e.g., BatterySoc)
/// Stores raw JSON data without specific parsing
/// </summary>
public record ProcessGenericTelemetryCommand : IRequest<ProcessGenericTelemetryResult>
{
    /// <summary>
    /// Raw JSON body from the telemetry endpoint
    /// </summary>
    public JsonElement RawBody { get; init; }

    /// <summary>
    /// Data type identifier (e.g., "BatterySoc")
    /// </summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>
    /// Telemetry source for activity tracking
    /// </summary>
    public TelemetrySource TelemetrySource { get; init; }
}

public class ProcessGenericTelemetryCommandHandler : IRequestHandler<ProcessGenericTelemetryCommand, ProcessGenericTelemetryResult>
{
    private readonly ISender _sender;
    private readonly ITelemetryDeduplicationService _deduplicationService;
    private readonly IDeviceActivityService _activityService;
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<ProcessGenericTelemetryCommandHandler> _logger;

    public ProcessGenericTelemetryCommandHandler(
        ISender sender,
        ITelemetryDeduplicationService deduplicationService,
        IDeviceActivityService activityService,
        IDemoDataService demoDataService,
        ILogger<ProcessGenericTelemetryCommandHandler> logger)
    {
        _sender = sender;
        _deduplicationService = deduplicationService;
        _activityService = activityService;
        _demoDataService = demoDataService;
        _logger = logger;
    }

    public async Task<ProcessGenericTelemetryResult> Handle(ProcessGenericTelemetryCommand request, CancellationToken cancellationToken)
    {
        var body = request.RawBody;
        var dataType = request.DataType;

        _logger.LogInformation("{DataType} telemetry received at {UtcNow}", dataType, DateTime.UtcNow);

        // 1) Get the inner "data" object
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            _logger.LogWarning("Received telemetry without 'data' object");
            return new ProcessGenericTelemetryResult
            {
                Success = false,
                Error = "Missing 'data' object"
            };
        }

        // 2) DeviceLocalTime comes from data.timestampUtc
        DateTime requestDate = DateTime.UtcNow;
        if (dataElement.TryGetProperty("timestampUtc", out var tsEl))
        {
            var tsStr = tsEl.GetString();
            if (!string.IsNullOrWhiteSpace(tsStr) &&
                DateTime.TryParse(tsStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                requestDate = parsed;
            }
            else
            {
                _logger.LogWarning("Could not parse timestampUtc: {TimestampUtc}", tsStr);
            }
        }

        // 3) ThingsBoardDeviceId comes from root level "deviceId" field
        Guid deviceId = Guid.Empty;
        if (body.TryGetProperty("deviceId", out var deviceIdElement))
        {
            var deviceIdStr = deviceIdElement.GetString();
            if (!string.IsNullOrWhiteSpace(deviceIdStr) && Guid.TryParse(deviceIdStr, out var parsedDeviceId))
            {
                deviceId = parsedDeviceId;
            }
            else
            {
                _logger.LogWarning("Invalid deviceId format: {DeviceId}", deviceIdStr);
                return new ProcessGenericTelemetryResult
                {
                    Success = false,
                    Error = "Invalid 'deviceId' format"
                };
            }
        }
        else
        {
            _logger.LogWarning("Missing deviceId in root level");
            return new ProcessGenericTelemetryResult
            {
                Success = false,
                Error = "Missing 'deviceId' in request"
            };
        }

        // Record device activity (updates heartbeat cache, sends SignalR if status changed)
        await _activityService.RecordActivityAsync(deviceId, request.TelemetrySource, requestDate);

        // 4) Check if this is a demo device - skip database storage for demo devices
        var isDemoDevice = await _demoDataService.IsDemoDeviceByThingsBoardIdAsync(deviceId, cancellationToken);
        if (isDemoDevice)
        {
            _logger.LogDebug(
                "Skipping {DataType} database storage for demo device {DeviceId} at {Timestamp}",
                dataType, deviceId, requestDate);
            return new ProcessGenericTelemetryResult
            {
                Success = true,
                Message = $"{dataType} telemetry received (demo device - not stored)",
                DeviceId = deviceId,
                DataDateTime = requestDate,
                IsDuplicate = false
            };
        }

        // 5) Check for duplicate telemetry
        var shouldProcess = await _deduplicationService.ShouldProcessTelemetryAsync(deviceId, dataType, requestDate);
        if (!shouldProcess)
        {
            _logger.LogInformation(
                "Skipping duplicate {DataType} telemetry for device {DeviceId} at {Timestamp}",
                dataType, deviceId, requestDate);
            return new ProcessGenericTelemetryResult
            {
                Success = true,
                Message = $"{dataType} telemetry skipped (duplicate)",
                DeviceId = deviceId,
                DataDateTime = requestDate,
                IsDuplicate = true
            };
        }

        // 5) Store the whole inner data object as JsonDocument
        var dataJson = JsonDocument.Parse(dataElement.GetRawText());

        // 6) Parse CreatedBy (data source identifier)
        string? createdBy = null;
        if (body.TryGetProperty("createdBy", out var createdByEl))
        {
            createdBy = createdByEl.GetString();
        }

        var command = new CreateDeviceDataCommand
        {
            DataType = dataType,
            DeviceId = deviceId,
            DeviceLocalTime = requestDate,
            Data = dataJson,
            CreatedBy = createdBy
        };

        var createdId = await _sender.Send(command, cancellationToken);

        return new ProcessGenericTelemetryResult
        {
            Success = true,
            Message = $"{dataType} telemetry received",
            CreatedId = createdId,
            DeviceId = deviceId,
            DataDateTime = requestDate,
            IsDuplicate = false
        };
    }
}
