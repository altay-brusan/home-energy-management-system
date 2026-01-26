using System.Globalization;
using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceData.Commands.CreateDevicePowerData;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceData.Commands.ProcessPowerFlowTelemetry;

/// <summary>
/// Result of processing power flow telemetry
/// </summary>
public record ProcessPowerFlowTelemetryResult
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
/// Command to process raw power flow telemetry from ThingsBoard
/// Handles JSON parsing, deduplication, and device activity recording
/// </summary>
public record ProcessPowerFlowTelemetryCommand : IRequest<ProcessPowerFlowTelemetryResult>
{
    /// <summary>
    /// Raw JSON body from the telemetry endpoint
    /// Expected format: { deviceId: string, data: { timestampUtc, pv, grid, load, battery, generator }, createdBy?: string }
    /// </summary>
    public JsonElement RawBody { get; init; }
}

public class ProcessPowerFlowTelemetryCommandHandler : IRequestHandler<ProcessPowerFlowTelemetryCommand, ProcessPowerFlowTelemetryResult>
{
    private readonly ISender _sender;
    private readonly ITelemetryDeduplicationService _deduplicationService;
    private readonly IDeviceActivityService _activityService;
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<ProcessPowerFlowTelemetryCommandHandler> _logger;

    public ProcessPowerFlowTelemetryCommandHandler(
        ISender sender,
        ITelemetryDeduplicationService deduplicationService,
        IDeviceActivityService activityService,
        IDemoDataService demoDataService,
        ILogger<ProcessPowerFlowTelemetryCommandHandler> logger)
    {
        _sender = sender;
        _deduplicationService = deduplicationService;
        _activityService = activityService;
        _demoDataService = demoDataService;
        _logger = logger;
    }

    public async Task<ProcessPowerFlowTelemetryResult> Handle(ProcessPowerFlowTelemetryCommand request, CancellationToken cancellationToken)
    {
        var body = request.RawBody;

        _logger.LogInformation("PowerFlow telemetry received at {UtcNow}", DateTime.UtcNow);

        // 1) Get the inner "data" object
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            _logger.LogWarning("Received telemetry without 'data' object");
            return new ProcessPowerFlowTelemetryResult
            {
                Success = false,
                Error = "Missing 'data' object"
            };
        }

        // 2) Parse timestampUtc
        DateTime dataDateTime = DateTime.UtcNow;
        if (dataElement.TryGetProperty("timestampUtc", out var tsEl))
        {
            var tsStr = tsEl.GetString();
            if (!string.IsNullOrWhiteSpace(tsStr) &&
                DateTime.TryParse(tsStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                dataDateTime = parsed;
            }
        }

        // 3) Parse ThingsBoardDeviceId from root level "deviceId" field
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
                return new ProcessPowerFlowTelemetryResult
                {
                    Success = false,
                    Error = "Invalid 'deviceId' format"
                };
            }
        }
        else
        {
            _logger.LogWarning("Missing deviceId in root level");
            return new ProcessPowerFlowTelemetryResult
            {
                Success = false,
                Error = "Missing 'deviceId' in request"
            };
        }

        // Record device activity (updates heartbeat cache, sends SignalR if status changed)
        await _activityService.RecordActivityAsync(deviceId, TelemetrySource.PowerFlow, dataDateTime);

        // 4) Check if this is a demo device - skip database storage for demo devices
        var isDemoDevice = await _demoDataService.IsDemoDeviceByThingsBoardIdAsync(deviceId, cancellationToken);
        if (isDemoDevice)
        {
            _logger.LogDebug(
                "Skipping PowerFlow database storage for demo device {DeviceId} at {Timestamp}",
                deviceId, dataDateTime);
            return new ProcessPowerFlowTelemetryResult
            {
                Success = true,
                Message = "PowerFlow telemetry received (demo device - not stored)",
                DeviceId = deviceId,
                DataDateTime = dataDateTime,
                IsDuplicate = false
            };
        }

        // 5) Check for duplicate telemetry
        var shouldProcess = await _deduplicationService.ShouldProcessTelemetryAsync(deviceId, DataTypes.PowerFlow, dataDateTime);
        if (!shouldProcess)
        {
            _logger.LogInformation(
                "Skipping duplicate PowerFlow telemetry for device {DeviceId} at {Timestamp}",
                deviceId, dataDateTime);
            return new ProcessPowerFlowTelemetryResult
            {
                Success = true,
                Message = "PowerFlow telemetry skipped (duplicate)",
                DeviceId = deviceId,
                DataDateTime = dataDateTime,
                IsDuplicate = true
            };
        }

        // 5) Parse version
        int? version = null;
        if (dataElement.TryGetProperty("version", out var versionEl))
        {
            version = versionEl.GetInt32();
        }

        // 6) Parse power values
        var powerValues = ParsePowerValues(dataElement);

        // 7) Parse CreatedBy (data source identifier)
        string? createdBy = null;
        if (body.TryGetProperty("createdBy", out var createdByEl))
        {
            createdBy = createdByEl.GetString();
        }

        // Create command for DevicePowerData table
        var command = new CreateDevicePowerDataCommand
        {
            DeviceId = deviceId,
            DeviceLocalTime = dataDateTime,
            DataType = DataTypes.PowerFlow,
            Version = version,
            PvTotalPowerW = powerValues.PvTotalPowerW,
            PvMppt1PowerW = powerValues.PvMppt1PowerW,
            PvMppt2PowerW = powerValues.PvMppt2PowerW,
            PvMppt3PowerW = powerValues.PvMppt3PowerW,
            PvMppt4PowerW = powerValues.PvMppt4PowerW,
            GridTotalPowerW = powerValues.GridTotalPowerW,
            LoadTotalPowerW = powerValues.LoadTotalPowerW,
            BatteryPowerW = powerValues.BatteryPowerW,
            GeneratorTotalPowerW = powerValues.GeneratorTotalPowerW,
            CreatedBy = createdBy
        };

        var createdId = await _sender.Send(command, cancellationToken);

        return new ProcessPowerFlowTelemetryResult
        {
            Success = true,
            Message = "PowerFlow telemetry received",
            CreatedId = createdId,
            DeviceId = deviceId,
            DataDateTime = dataDateTime,
            IsDuplicate = false
        };
    }

    private static PowerValues ParsePowerValues(JsonElement dataElement)
    {
        var result = new PowerValues();

        // Parse PV power
        if (dataElement.TryGetProperty("pv", out var pvElement))
        {
            if (pvElement.TryGetProperty("totalPowerW", out var pvTotalEl))
            {
                result.PvTotalPowerW = pvTotalEl.GetDouble();
            }

            // Parse MPPT array
            if (pvElement.TryGetProperty("mppt", out var mpptArray) &&
                mpptArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var mppt in mpptArray.EnumerateArray())
                {
                    if (mppt.TryGetProperty("id", out var idEl) &&
                        mppt.TryGetProperty("powerW", out var powerEl))
                    {
                        var id = idEl.GetInt32();
                        var power = powerEl.GetDouble();

                        switch (id)
                        {
                            case 1: result.PvMppt1PowerW = power; break;
                            case 2: result.PvMppt2PowerW = power; break;
                            case 3: result.PvMppt3PowerW = power; break;
                            case 4: result.PvMppt4PowerW = power; break;
                        }
                    }
                }
            }
        }

        // Parse Grid power
        if (dataElement.TryGetProperty("grid", out var gridElement) &&
            gridElement.TryGetProperty("totalPowerW", out var gridPowerEl))
        {
            result.GridTotalPowerW = gridPowerEl.GetDouble();
        }

        // Parse Load power
        if (dataElement.TryGetProperty("load", out var loadElement) &&
            loadElement.TryGetProperty("totalPowerW", out var loadPowerEl))
        {
            result.LoadTotalPowerW = loadPowerEl.GetDouble();
        }

        // Parse Battery power
        if (dataElement.TryGetProperty("battery", out var batteryElement) &&
            batteryElement.TryGetProperty("powerW", out var batteryPowerEl))
        {
            result.BatteryPowerW = batteryPowerEl.GetDouble();
        }

        // Parse Generator power
        if (dataElement.TryGetProperty("generator", out var generatorElement) &&
            generatorElement.TryGetProperty("totalPowerW", out var generatorPowerEl))
        {
            result.GeneratorTotalPowerW = generatorPowerEl.GetDouble();
        }

        return result;
    }

    private class PowerValues
    {
        public double? PvTotalPowerW { get; set; }
        public double? PvMppt1PowerW { get; set; }
        public double? PvMppt2PowerW { get; set; }
        public double? PvMppt3PowerW { get; set; }
        public double? PvMppt4PowerW { get; set; }
        public double? GridTotalPowerW { get; set; }
        public double? LoadTotalPowerW { get; set; }
        public double? BatteryPowerW { get; set; }
        public double? GeneratorTotalPowerW { get; set; }
    }
}
