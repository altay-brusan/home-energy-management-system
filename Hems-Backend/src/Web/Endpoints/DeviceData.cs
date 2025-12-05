using System.Globalization;
using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceData.Commands.BulkCreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceEnergyData;
using Hems_Backend.Application.DeviceData.Commands.CreateDevicePowerData;
using Hems_Backend.Application.DeviceData.Queries.GetDeviceData;
using Hems_Backend.Application.Devices.Queries.GetDeviceHeartbeatStatus;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;
using Hems_Backend.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // make sure this is present
using Microsoft.AspNetCore.Mvc;

namespace Hems_Backend.Web.Endpoints;

public class DeviceData : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Device telemetry endpoints (no authorization - devices authenticate via policy)
        app.MapGroup(this)
            .MapPost(ReceivePowerFlowTelemetry, "PowerFlow")
            .MapPost(ReceiveBatterySocTelemetry, "BatterySoc")
            .MapPost(ReceiveDailyEnergyFlowTelemetry, "DailyEnergyFlow")
            .MapPost(ReceiveTotalEnergyFlowTelemetry, "TotalEnergyFlow")
            .MapPost(ReceiveHeartbeat, "Heartbeat");

        // User endpoints (require user authorization)
        app.MapGroup(this)
            //.RequireAuthorization()
            .MapGet(GetDeviceData)
            .MapGet(GetLivePowerFlow, "PowerFlow/live/{deviceId:guid}")
            .MapGet(GetDeviceHeartbeatStatus, "Status/{deviceId:guid}");
    }

    /// <summary>
    /// Receives lightweight heartbeat signal from devices (every 1 minute)
    /// No authentication required - lightweight endpoint
    /// </summary>
    public async Task<IResult> ReceiveHeartbeat(
        HeartbeatDto request,
        IDeviceHeartbeatCache heartbeatCache,
        ILogger<DeviceData> logger)
    {
        try
        {
            // Parse deviceId from request
            if (!Guid.TryParse(request.DeviceId, out var deviceId))
            {
                logger.LogWarning("Invalid deviceId in heartbeat: {DeviceId}", request.DeviceId);
                return Results.BadRequest("Invalid deviceId format");
            }

            // Record heartbeat with timestamp (handles NTP sync validation)
            await heartbeatCache.RecordHeartbeatAsync(deviceId, request.Timestamp);

            logger.LogDebug("Heartbeat received from device {DeviceId} at {Timestamp}", 
                deviceId, request.Timestamp);

            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing heartbeat");
            return Results.StatusCode(500);
        }
    }

    public async Task<IResult> GetLivePowerFlow(
    Guid deviceId,
    IPowerFlowService powerFlowService,
    ILogger<DeviceData> logger,
    CancellationToken ct)
    {
        logger.LogInformation("?? Live PowerFlow requested for ThingsBoardDeviceId {ThingsBoardDeviceId}", deviceId);

        // Call our RPC-based service (TB -> Node-RED -> inverter)
        var dto = await powerFlowService.GetPowerFlowAsync(deviceId, ct);

        if (dto == null)
        {
            logger.LogWarning("PowerFlow could not be retrieved for ThingsBoardDeviceId {ThingsBoardDeviceId}", deviceId);
            return Results.Problem(
                detail: "PowerFlow could not be retrieved (TB RPC failed or device offline).",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(dto);
    }

    /// <summary>
    /// Gets current heartbeat status of a device
    /// </summary>
    public async Task<DeviceHeartbeatStatusDto> GetDeviceHeartbeatStatus(
        ISender sender,
        Guid deviceId)
    {
        return await sender.Send(new GetDeviceHeartbeatStatusQuery { DeviceId = deviceId });
    }

    public async Task<PaginatedList<DeviceDataDto>> GetDeviceData(
        ISender sender,
        [AsParameters] GetDeviceDataQuery query)
    {
        return await sender.Send(query);
    }

    [Authorize(Policies.DeviceOnly)]
    public async Task<int> CreateDeviceData(
        ISender sender,
        CreateDeviceDataCommand command)
    {
        return await sender.Send(command);
    }

    [Authorize(Policies.DeviceOnly)]
    public async Task<BulkCreateDeviceDataResult> BulkCreateDeviceData(
        ISender sender,
        BulkCreateDeviceDataCommand command)
    {
        return await sender.Send(command);
    }

    [Authorize(Policies.DeviceOnly)]
    public async Task<int> CreateDeviceEnergyData(
        ISender sender,
        CreateDeviceEnergyDataCommand command)
    {
        return await sender.Send(command);
    }


public async Task<IResult> ReceiveBatterySocTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService)
{
    return await ProcessTelemetry(body, "BatterySoc", logger, sender, deduplicationService);
}

public async Task<IResult> ReceivePowerFlowTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService)
{
    return await ProcessPowerFlowTelemetry(body, DataTypes.PowerFlow, logger, sender, deduplicationService);
}

public async Task<IResult> ReceiveDailyEnergyFlowTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService)
{
    return await ProcessEnergyFlowTelemetry(body, DataTypes.DailyCumulativeEnergy, logger, sender, deduplicationService);
}

public async Task<IResult> ReceiveTotalEnergyFlowTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService)
{
    return await ProcessEnergyFlowTelemetry(body, DataTypes.LifetimeEnergy, logger, sender, deduplicationService);
}

private async Task<IResult> ProcessEnergyFlowTelemetry(
    JsonElement body,
    string dataType,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService)
{
    try
    {
        logger.LogInformation(
            "?? {DataType} energy flow telemetry received at {UtcNow}",
            dataType,
            DateTime.UtcNow);

        // 1) Get the inner "data" object
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            logger.LogWarning("Received telemetry without 'data' object");
            return Results.Problem("Missing 'data' object", statusCode: 400);
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
                logger.LogWarning("Invalid deviceId format: {DeviceId}", deviceIdStr);
                return Results.Problem("Invalid 'deviceId' format", statusCode: 400);
            }
        }
        else
        {
            logger.LogWarning("Missing deviceId in root level");
            return Results.Problem("Missing 'deviceId' in request", statusCode: 400);
        }

        // 4) Check for duplicate telemetry
        var shouldProcess = await deduplicationService.ShouldProcessTelemetryAsync(deviceId, dataType, dataDateTime);
        if (!shouldProcess)
        {
            logger.LogInformation(
                "?? Skipping duplicate {DataType} telemetry for device {DeviceId} at {Timestamp}",
                dataType, deviceId, dataDateTime);
            return Results.Ok(new
            {
                message = $"{dataType} telemetry skipped (duplicate)",
                deviceId = deviceId,
                dataDateTime = dataDateTime,
                duplicate = true
            });
        }

        // 5) Parse energy values based on structure
        double? pvTotalEnergy = null;
        double? pvMppt1Energy = null;
        double? pvMppt2Energy = null;
        double? pvMppt3Energy = null;
        double? pvMppt4Energy = null;
        double? gridImportEnergy = null;
        double? gridExportEnergy = null;
        double? batteryChargeEnergy = null;
        double? batteryDischargeEnergy = null;
        double? loadEnergy = null;
        double? generatorEnergy = null;

        // Parse PV energy
        if (dataElement.TryGetProperty("pv", out var pvElement))
        {
            if (pvElement.TryGetProperty("totalEnergyKWh", out var pvTotalEl))
            {
                pvTotalEnergy = pvTotalEl.GetDouble();
            }
            else if (pvElement.TryGetProperty("totalEnergyKwh", out var pvTotalEl2))
            {
                pvTotalEnergy = pvTotalEl2.GetDouble();
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
                            case 1: pvMppt1Energy = energy; break;
                            case 2: pvMppt2Energy = energy; break;
                            case 3: pvMppt3Energy = energy; break;
                            case 4: pvMppt4Energy = energy; break;
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
                            case 1: pvMppt1Energy = energy; break;
                            case 2: pvMppt2Energy = energy; break;
                            case 3: pvMppt3Energy = energy; break;
                            case 4: pvMppt4Energy = energy; break;
                        }
                    }
                }
            }
        }

        // Parse Grid energy
        if (dataElement.TryGetProperty("grid", out var gridElement))
        {
            if (gridElement.TryGetProperty("importEnergyKWh", out var importEl))
                gridImportEnergy = importEl.GetDouble();
            else if (gridElement.TryGetProperty("importTotalEnergyKWh", out var importTotalEl))
                gridImportEnergy = importTotalEl.GetDouble();

            if (gridElement.TryGetProperty("exportEnergyKWh", out var exportEl))
                gridExportEnergy = exportEl.GetDouble();
            else if (gridElement.TryGetProperty("exportTotalEnergyKWh", out var exportTotalEl))
                gridExportEnergy = exportTotalEl.GetDouble();
        }

        // Parse Battery energy
        if (dataElement.TryGetProperty("battery", out var batteryElement))
        {
            if (batteryElement.TryGetProperty("chargeEnergyKWh", out var chargeEl))
                batteryChargeEnergy = chargeEl.GetDouble();
            else if (batteryElement.TryGetProperty("totalChargeEnergyKWh", out var chargeTotalEl))
                batteryChargeEnergy = chargeTotalEl.GetDouble();

            if (batteryElement.TryGetProperty("dischargeEnergyKWh", out var dischargeEl))
                batteryDischargeEnergy = dischargeEl.GetDouble();
            else if (batteryElement.TryGetProperty("totalDischargeEnergyKWh", out var dischargeTotalEl))
                batteryDischargeEnergy = dischargeTotalEl.GetDouble();
        }

        // Parse Load energy
        if (dataElement.TryGetProperty("loadEnergyKWh", out var loadEl))
        {
            loadEnergy = loadEl.GetDouble();
        }
        else if (dataElement.TryGetProperty("load", out var loadElement) &&
                 loadElement.TryGetProperty("totalEnergyKWh", out var loadTotalEl))
        {
            loadEnergy = loadTotalEl.GetDouble();
        }

        // Parse Generator energy
        if (dataElement.TryGetProperty("generator", out var generatorElement))
        {
            if (generatorElement.TryGetProperty("energyKWh", out var genEl))
                generatorEnergy = genEl.GetDouble();
            else if (generatorElement.TryGetProperty("totalEnergyKWh", out var genTotalEl))
                generatorEnergy = genTotalEl.GetDouble();
        }

        // Create command for DeviceEnergyData table
        var command = new CreateDeviceEnergyDataCommand
        {
            DeviceId = deviceId,
            DataDateTime = dataDateTime,
            DataType = dataType,
            PvEnergyKWhTotal = pvTotalEnergy ?? 0,
            PvEnergyKWhToday = pvTotalEnergy ?? 0,
            PvMppt1EnergyKWh = pvMppt1Energy,
            PvMppt2EnergyKWh = pvMppt2Energy,
            PvMppt3EnergyKWh = pvMppt3Energy,
            PvMppt4EnergyKWh = pvMppt4Energy,
            GridImportEnergyKWhTotal = gridImportEnergy ?? 0,
            GridImportEnergyKWhToday = gridImportEnergy ?? 0,
            GridExportEnergyKWhTotal = gridExportEnergy ?? 0,
            GridExportEnergyKWhToday = gridExportEnergy ?? 0,
            BatteryChargeEnergyKWhTotal = batteryChargeEnergy ?? 0,
            BatteryChargeEnergyKWhToday = batteryChargeEnergy ?? 0,
            BatteryDischargeEnergyKWhTotal = batteryDischargeEnergy ?? 0,
            BatteryDischargeEnergyKWhToday = batteryDischargeEnergy ?? 0,
            LoadEnergyKWhTotal = loadEnergy ?? 0,
            LoadEnergyKWhToday = loadEnergy ?? 0,
            GeneratorEnergyKWhTotal = generatorEnergy ?? 0,
            GeneratorEnergyKWhToday = generatorEnergy ?? 0
        };

        var createdId = await sender.Send(command);

        return Results.Ok(new
        {
            message = $"{dataType} energy flow telemetry received",
            id = createdId,
            deviceId = deviceId,
            dataDateTime = dataDateTime
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling {DataType} energy flow telemetry", dataType);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
}

private async Task<IResult> ProcessTelemetry(
    JsonElement body,
    string dataType,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService)
{
    try
    {
        logger.LogInformation(
            "?? {DataType} telemetry received at {UtcNow}",
            dataType,
            DateTime.UtcNow);

        // 1) Get the inner "data" object
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            logger.LogWarning("Received telemetry without 'data' object");
            return Results.Problem("Missing 'data' object", statusCode: 400);
        }

        // 2) RequestDate comes from data.timestampUtc
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
                logger.LogWarning("Could not parse timestampUtc: {TimestampUtc}", tsStr);
            }
        }

        // 3) ThingsBoardDeviceId comes from root level "deviceId" field (string ? Guid)
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
                logger.LogWarning("Invalid deviceId format: {DeviceId}", deviceIdStr);
                return Results.Problem("Invalid 'deviceId' format", statusCode: 400);
            }
        }
        else
        {
            logger.LogWarning("Missing deviceId in root level");
            return Results.Problem("Missing 'deviceId' in request", statusCode: 400);
        }

        // 4) Check for duplicate telemetry
        var shouldProcess = await deduplicationService.ShouldProcessTelemetryAsync(deviceId, dataType, requestDate);
        if (!shouldProcess)
        {
            logger.LogInformation(
                "?? Skipping duplicate {DataType} telemetry for device {DeviceId} at {Timestamp}",
                dataType, deviceId, requestDate);
            return Results.Ok(new
            {
                message = $"{dataType} telemetry skipped (duplicate)",
                id = 0,
                duplicate = true
            });
        }

        // 5) Store the whole inner data object as JsonDocument
        var dataJson = JsonDocument.Parse(dataElement.GetRawText());

        var command = new CreateDeviceDataCommand
        {
            DataType = dataType,
            DeviceId = deviceId,
            RequestDate = requestDate,
            Data = dataJson
        };

        var createdId = await sender.Send(command);

        return Results.Ok(new
        {
            message = $"{dataType} telemetry received",
            id = createdId
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling {DataType} telemetry", dataType);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
}

private async Task<IResult> ProcessPowerFlowTelemetry(
    JsonElement body,
    string dataType,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService)
{
    try
    {
        logger.LogInformation(
            "?? {DataType} power flow telemetry received at {UtcNow}",
            dataType,
            DateTime.UtcNow);

        // 1) Get the inner "data" object
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            logger.LogWarning("Received telemetry without 'data' object");
            return Results.Problem("Missing 'data' object", statusCode: 400);
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
                logger.LogWarning("Invalid deviceId format: {DeviceId}", deviceIdStr);
                return Results.Problem("Invalid 'deviceId' format", statusCode: 400);
            }
        }
        else
        {
            logger.LogWarning("Missing deviceId in root level");
            return Results.Problem("Missing 'deviceId' in request", statusCode: 400);
        }

        // 4) Check for duplicate telemetry
        var shouldProcess = await deduplicationService.ShouldProcessTelemetryAsync(deviceId, dataType, dataDateTime);
        if (!shouldProcess)
        {
            logger.LogInformation(
                "?? Skipping duplicate {DataType} telemetry for device {DeviceId} at {Timestamp}",
                dataType, deviceId, dataDateTime);
            return Results.Ok(new
            {
                message = $"{dataType} telemetry skipped (duplicate)",
                deviceId = deviceId,
                dataDateTime = dataDateTime,
                duplicate = true
            });
        }

        // 5) Parse version
        int? version = null;
        if (dataElement.TryGetProperty("version", out var versionEl))
        {
            version = versionEl.GetInt32();
        }

        // 6) Parse power values
        double? pvTotalPowerW = null;
        double? pvMppt1PowerW = null;
        double? pvMppt2PowerW = null;
        double? pvMppt3PowerW = null;
        double? pvMppt4PowerW = null;
        double? gridTotalPowerW = null;
        double? loadTotalPowerW = null;
        double? batteryPowerW = null;
        double? generatorTotalPowerW = null;

        // Parse PV power
        if (dataElement.TryGetProperty("pv", out var pvElement))
        {
            if (pvElement.TryGetProperty("totalPowerW", out var pvTotalEl))
            {
                pvTotalPowerW = pvTotalEl.GetDouble();
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
                            case 1: pvMppt1PowerW = power; break;
                            case 2: pvMppt2PowerW = power; break;
                            case 3: pvMppt3PowerW = power; break;
                            case 4: pvMppt4PowerW = power; break;
                        }
                    }
                }
            }
        }

        // Parse Grid power
        if (dataElement.TryGetProperty("grid", out var gridElement) &&
            gridElement.TryGetProperty("totalPowerW", out var gridPowerEl))
        {
            gridTotalPowerW = gridPowerEl.GetDouble();
        }

        // Parse Load power
        if (dataElement.TryGetProperty("load", out var loadElement) &&
            loadElement.TryGetProperty("totalPowerW", out var loadPowerEl))
        {
            loadTotalPowerW = loadPowerEl.GetDouble();
        }

        // Parse Battery power
        if (dataElement.TryGetProperty("battery", out var batteryElement) &&
            batteryElement.TryGetProperty("powerW", out var batteryPowerEl))
        {
            batteryPowerW = batteryPowerEl.GetDouble();
        }

        // Parse Generator power
        if (dataElement.TryGetProperty("generator", out var generatorElement) &&
            generatorElement.TryGetProperty("totalPowerW", out var generatorPowerEl))
        {
            generatorTotalPowerW = generatorPowerEl.GetDouble();
        }

        // Create command for DevicePowerData table
        var command = new CreateDevicePowerDataCommand
        {
            DeviceId = deviceId,
            DataDateTime = dataDateTime,
            DataType = dataType,
            Version = version,
            PvTotalPowerW = pvTotalPowerW,
            PvMppt1PowerW = pvMppt1PowerW,
            PvMppt2PowerW = pvMppt2PowerW,
            PvMppt3PowerW = pvMppt3PowerW,
            PvMppt4PowerW = pvMppt4PowerW,
            GridTotalPowerW = gridTotalPowerW,
            LoadTotalPowerW = loadTotalPowerW,
            BatteryPowerW = batteryPowerW,
            GeneratorTotalPowerW = generatorTotalPowerW
        };

        var createdId = await sender.Send(command);

        return Results.Ok(new
        {
            message = $"{dataType} power flow telemetry received",
            id = createdId,
            deviceId = deviceId,
            dataDateTime = dataDateTime
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling {DataType} power flow telemetry", dataType);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
}

}

/// <summary>
/// DTO for device heartbeat
/// </summary>
public record HeartbeatDto
{
    /// <summary>
    /// ThingsBoard Device ID (string format)
    /// </summary>
    public string DeviceId { get; init; } = string.Empty;
    
    /// <summary>
    /// Timestamp from device (for NTP sync validation)
    /// </summary>
    public DateTime Timestamp { get; init; }
}
