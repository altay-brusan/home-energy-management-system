using System.Globalization;
using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceEnergyData;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceData.Commands.ProcessEnergyFlowTelemetry;

/// <summary>
/// Result of processing energy flow telemetry
/// </summary>
public record ProcessEnergyFlowTelemetryResult
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
/// Command to process raw energy flow telemetry from ThingsBoard
/// Handles both DailyEnergyFlow and TotalEnergyFlow (LifetimeEnergy)
/// </summary>
public record ProcessEnergyFlowTelemetryCommand : IRequest<ProcessEnergyFlowTelemetryResult>
{
    /// <summary>
    /// Raw JSON body from the telemetry endpoint
    /// </summary>
    public JsonElement RawBody { get; init; }

    /// <summary>
    /// Data type: DataTypes.DailyCumulativeEnergy or DataTypes.LifetimeEnergy
    /// </summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>
    /// Telemetry source for activity tracking
    /// </summary>
    public TelemetrySource TelemetrySource { get; init; }
}

public class ProcessEnergyFlowTelemetryCommandHandler : IRequestHandler<ProcessEnergyFlowTelemetryCommand, ProcessEnergyFlowTelemetryResult>
{
    private readonly ISender _sender;
    private readonly ITelemetryDeduplicationService _deduplicationService;
    private readonly IDeviceActivityService _activityService;
    private readonly ILogger<ProcessEnergyFlowTelemetryCommandHandler> _logger;

    public ProcessEnergyFlowTelemetryCommandHandler(
        ISender sender,
        ITelemetryDeduplicationService deduplicationService,
        IDeviceActivityService activityService,
        ILogger<ProcessEnergyFlowTelemetryCommandHandler> logger)
    {
        _sender = sender;
        _deduplicationService = deduplicationService;
        _activityService = activityService;
        _logger = logger;
    }

    public async Task<ProcessEnergyFlowTelemetryResult> Handle(ProcessEnergyFlowTelemetryCommand request, CancellationToken cancellationToken)
    {
        var body = request.RawBody;
        var dataType = request.DataType;

        _logger.LogInformation("{DataType} energy flow telemetry received at {UtcNow}", dataType, DateTime.UtcNow);

        // 1) Get the inner "data" object
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            _logger.LogWarning("Received telemetry without 'data' object");
            return new ProcessEnergyFlowTelemetryResult
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
                return new ProcessEnergyFlowTelemetryResult
                {
                    Success = false,
                    Error = "Invalid 'deviceId' format"
                };
            }
        }
        else
        {
            _logger.LogWarning("Missing deviceId in root level");
            return new ProcessEnergyFlowTelemetryResult
            {
                Success = false,
                Error = "Missing 'deviceId' in request"
            };
        }

        // Record device activity (updates heartbeat cache, sends SignalR if status changed)
        await _activityService.RecordActivityAsync(deviceId, request.TelemetrySource, dataDateTime);

        // 4) Check for duplicate telemetry
        var shouldProcess = await _deduplicationService.ShouldProcessTelemetryAsync(deviceId, dataType, dataDateTime);
        if (!shouldProcess)
        {
            _logger.LogInformation(
                "Skipping duplicate {DataType} telemetry for device {DeviceId} at {Timestamp}",
                dataType, deviceId, dataDateTime);
            return new ProcessEnergyFlowTelemetryResult
            {
                Success = true,
                Message = $"{dataType} telemetry skipped (duplicate)",
                DeviceId = deviceId,
                DataDateTime = dataDateTime,
                IsDuplicate = true
            };
        }

        // 5) Parse energy values
        var energyValues = ParseEnergyValues(dataElement);

        // 6) Parse CreatedBy (data source identifier)
        string? createdBy = null;
        if (body.TryGetProperty("createdBy", out var createdByEl))
        {
            createdBy = createdByEl.GetString();
        }

        // Create command for DeviceEnergyData table
        // Set Today fields ONLY for DailyCumulativeEnergy, Total fields ONLY for LifetimeEnergy
        var isDailyData = dataType == DataTypes.DailyCumulativeEnergy;

        var command = new CreateDeviceEnergyDataCommand
        {
            DeviceId = deviceId,
            DeviceLocalTime = dataDateTime,
            DataType = dataType,

            // Today fields - only populated for Daily data
            PvEnergyKWhToday = isDailyData ? energyValues.PvTotalEnergy : null,
            GridImportEnergyKWhToday = isDailyData ? energyValues.GridImportEnergy : null,
            GridExportEnergyKWhToday = isDailyData ? energyValues.GridExportEnergy : null,
            BatteryChargeEnergyKWhToday = isDailyData ? energyValues.BatteryChargeEnergy : null,
            BatteryDischargeEnergyKWhToday = isDailyData ? energyValues.BatteryDischargeEnergy : null,
            LoadEnergyKWhToday = isDailyData ? energyValues.LoadEnergy : null,
            GeneratorEnergyKWhToday = isDailyData ? energyValues.GeneratorEnergy : null,

            // Total fields - only populated for Lifetime data
            PvEnergyKWhTotal = !isDailyData ? energyValues.PvTotalEnergy : null,
            GridImportEnergyKWhTotal = !isDailyData ? energyValues.GridImportEnergy : null,
            GridExportEnergyKWhTotal = !isDailyData ? energyValues.GridExportEnergy : null,
            BatteryChargeEnergyKWhTotal = !isDailyData ? energyValues.BatteryChargeEnergy : null,
            BatteryDischargeEnergyKWhTotal = !isDailyData ? energyValues.BatteryDischargeEnergy : null,
            LoadEnergyKWhTotal = !isDailyData ? energyValues.LoadEnergy : null,
            GeneratorEnergyKWhTotal = !isDailyData ? energyValues.GeneratorEnergy : null,

            // MPPT values (same for both - they're always the same granularity as the data type)
            PvMppt1EnergyKWh = energyValues.PvMppt1Energy,
            PvMppt2EnergyKWh = energyValues.PvMppt2Energy,
            PvMppt3EnergyKWh = energyValues.PvMppt3Energy,
            PvMppt4EnergyKWh = energyValues.PvMppt4Energy,

            CreatedBy = createdBy
        };

        var createdId = await _sender.Send(command, cancellationToken);

        return new ProcessEnergyFlowTelemetryResult
        {
            Success = true,
            Message = $"{dataType} energy flow telemetry received",
            CreatedId = createdId,
            DeviceId = deviceId,
            DataDateTime = dataDateTime,
            IsDuplicate = false
        };
    }

    private static EnergyValues ParseEnergyValues(JsonElement dataElement)
    {
        var result = new EnergyValues();

        // Parse PV energy
        if (dataElement.TryGetProperty("pv", out var pvElement))
        {
            if (pvElement.TryGetProperty("totalEnergyKWh", out var pvTotalEl))
            {
                result.PvTotalEnergy = pvTotalEl.GetDouble();
            }
            else if (pvElement.TryGetProperty("totalEnergyKwh", out var pvTotalEl2))
            {
                result.PvTotalEnergy = pvTotalEl2.GetDouble();
            }

            // Parse MPPT array
            if (pvElement.TryGetProperty("mppt", out var mpptArray) &&
                mpptArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var mppt in mpptArray.EnumerateArray())
                {
                    if (mppt.TryGetProperty("id", out var idEl) &&
                        mppt.TryGetProperty("energyKWh", out var energyEl))
                    {
                        var id = idEl.GetInt32();
                        var energy = energyEl.GetDouble();

                        switch (id)
                        {
                            case 1: result.PvMppt1Energy = energy; break;
                            case 2: result.PvMppt2Energy = energy; break;
                            case 3: result.PvMppt3Energy = energy; break;
                            case 4: result.PvMppt4Energy = energy; break;
                        }
                    }
                    else if (mppt.TryGetProperty("id", out var idEl2) &&
                             mppt.TryGetProperty("totalEnergyKWh", out var energyEl2))
                    {
                        // Handle TotalEnergyFlowState format
                        var id = idEl2.GetInt32();
                        var energy = energyEl2.GetDouble();

                        switch (id)
                        {
                            case 1: result.PvMppt1Energy = energy; break;
                            case 2: result.PvMppt2Energy = energy; break;
                            case 3: result.PvMppt3Energy = energy; break;
                            case 4: result.PvMppt4Energy = energy; break;
                        }
                    }
                }
            }
        }

        // Parse Grid energy
        if (dataElement.TryGetProperty("grid", out var gridElement))
        {
            if (gridElement.TryGetProperty("importEnergyKWh", out var importEl))
                result.GridImportEnergy = importEl.GetDouble();
            else if (gridElement.TryGetProperty("importTotalEnergyKWh", out var importTotalEl))
                result.GridImportEnergy = importTotalEl.GetDouble();

            if (gridElement.TryGetProperty("exportEnergyKWh", out var exportEl))
                result.GridExportEnergy = exportEl.GetDouble();
            else if (gridElement.TryGetProperty("exportTotalEnergyKWh", out var exportTotalEl))
                result.GridExportEnergy = exportTotalEl.GetDouble();
        }

        // Parse Battery energy
        if (dataElement.TryGetProperty("battery", out var batteryElement))
        {
            if (batteryElement.TryGetProperty("chargeEnergyKWh", out var chargeEl))
                result.BatteryChargeEnergy = chargeEl.GetDouble();
            else if (batteryElement.TryGetProperty("totalChargeEnergyKWh", out var chargeTotalEl))
                result.BatteryChargeEnergy = chargeTotalEl.GetDouble();

            if (batteryElement.TryGetProperty("dischargeEnergyKWh", out var dischargeEl))
                result.BatteryDischargeEnergy = dischargeEl.GetDouble();
            else if (batteryElement.TryGetProperty("totalDischargeEnergyKWh", out var dischargeTotalEl))
                result.BatteryDischargeEnergy = dischargeTotalEl.GetDouble();
        }

        // Parse Load energy
        if (dataElement.TryGetProperty("loadEnergyKWh", out var loadEl))
        {
            result.LoadEnergy = loadEl.GetDouble();
        }
        else if (dataElement.TryGetProperty("load", out var loadElement) &&
                 loadElement.TryGetProperty("totalEnergyKWh", out var loadTotalEl))
        {
            result.LoadEnergy = loadTotalEl.GetDouble();
        }

        // Parse Generator energy
        if (dataElement.TryGetProperty("generator", out var generatorElement))
        {
            if (generatorElement.TryGetProperty("energyKWh", out var genEl))
                result.GeneratorEnergy = genEl.GetDouble();
            else if (generatorElement.TryGetProperty("totalEnergyKWh", out var genTotalEl))
                result.GeneratorEnergy = genTotalEl.GetDouble();
        }

        return result;
    }

    private class EnergyValues
    {
        public double? PvTotalEnergy { get; set; }
        public double? PvMppt1Energy { get; set; }
        public double? PvMppt2Energy { get; set; }
        public double? PvMppt3Energy { get; set; }
        public double? PvMppt4Energy { get; set; }
        public double? GridImportEnergy { get; set; }
        public double? GridExportEnergy { get; set; }
        public double? BatteryChargeEnergy { get; set; }
        public double? BatteryDischargeEnergy { get; set; }
        public double? LoadEnergy { get; set; }
        public double? GeneratorEnergy { get; set; }
    }
}
