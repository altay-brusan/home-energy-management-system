using System.Globalization;
using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceData.Commands.BulkCreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceEnergyData;
using Hems_Backend.Application.DeviceData.Commands.CreateDevicePowerData;
using Hems_Backend.Application.DeviceData.Queries.GetDeviceData;
using Hems_Backend.Application.DeviceData.Queries.GetDeviceEnergyData;
using Hems_Backend.Application.Devices.Queries.GetDeviceHeartbeatStatus;
using Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;
using Hems_Backend.Application.DeviceNotifications.Common.Models;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Infrastructure.Hubs;
using Hems_Backend.Web.Infrastructure;
using Hems_Backend.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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
            .MapPost(ReceiveHeartbeat, "Heartbeat")
            .MapPost(ReceiveAlarmTelemetry, "Alarm");

        // User endpoints (require user authorization)
        app.MapGroup(this)
            //.RequireAuthorization()
            .MapGet(GetDeviceData)
            .MapGet(GetLivePowerFlow, "PowerFlow/live/{deviceId:guid}")
            .MapGet(GetDeviceHeartbeatStatus, "Status/{deviceId:guid}")
            .MapGet(GetLiveDailyEnergyFlow, "DailyEnergyFlow/live/{deviceId:guid}")
            .MapGet(GetLiveTotalEnergyFlow, "TotalEnergyFlow/live/{deviceId:guid}");
    }

    /// <summary>
    /// Receives lightweight heartbeat signal from devices (every 1 minute)
    /// No authentication required - lightweight endpoint
    /// </summary>
    public async Task<IResult> ReceiveHeartbeat(
        HeartbeatDto request,
        IDeviceActivityService activityService,
        ILogger<DeviceData> logger)
    {
        try
        {
            // Use centralized activity service to record heartbeat
            var result = await activityService.RecordActivityAsync(
                request.DeviceId,
                TelemetrySource.Heartbeat,
                request.Timestamp);

            if (!result.Success)
            {
                logger.LogWarning("Failed to record heartbeat: {Error}", result.ErrorMessage);
                return Results.BadRequest(result.ErrorMessage);
            }

            logger.LogDebug("Heartbeat received from device {DeviceId} at {Timestamp}",
                result.ThingsBoardDeviceId, request.Timestamp);

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

    /// <summary>
    /// Gets daily energy data for a device
    /// </summary>
    /// <param name="deviceId">ThingsBoard Device ID</param>
    /// <param name="date">Optional: Filter by specific date (yyyy-MM-dd)</param>
    /// <param name="startDate">Optional: Start date for range filter</param>
    /// <param name="endDate">Optional: End date for range filter</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    public async Task<PaginatedList<DeviceEnergyDataDto>> GetDailyEnergyFlow(
        ISender sender,
        Guid deviceId,
        DateTime? date = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = new GetDeviceEnergyDataQuery
        {
            DeviceId = deviceId,
            DataType = DataTypes.DailyCumulativeEnergy,
            Date = date,
            StartDate = startDate,
            EndDate = endDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return await sender.Send(query);
    }

    /// <summary>
    /// Gets live daily energy data from device via RPC.
    /// FOR TEST ONLY - DO NOT USE IN PRODUCTION
    /// </summary>
    public async Task<IResult> GetLiveDailyEnergyFlow(
        Guid deviceId,
        IEnergyDataCollectionService energyService,
        IDeviceMappingService deviceMapping,
        ILogger<DeviceData> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Live DailyEnergyFlow requested for DeviceId {DeviceId}", deviceId);

        // Map internal deviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await deviceMapping.GetThingsBoardDeviceIdAsync(deviceId, ct);
        if (thingsBoardDeviceId == null)
        {
            logger.LogWarning("Device mapping not found for DeviceId {DeviceId}", deviceId);
            return Results.Problem(
                detail: $"Device not found: {deviceId}",
                statusCode: StatusCodes.Status404NotFound);
        }

        var result = await energyService.FetchDailyEnergyViaRpcAsync(thingsBoardDeviceId.Value, ct);

        if (result == null)
        {
            logger.LogWarning("DailyEnergyFlow could not be retrieved for DeviceId {DeviceId} (TB: {ThingsBoardDeviceId})",
                deviceId, thingsBoardDeviceId);
            return Results.Problem(
                detail: "DailyEnergyFlow could not be retrieved (RPC failed or device offline).",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(result);
    }

    /// <summary>
    /// Gets live total (lifetime) energy data from device via RPC.
    /// FOR TEST ONLY - DO NOT USE IN PRODUCTION
    /// </summary>
    public async Task<IResult> GetLiveTotalEnergyFlow(
        Guid deviceId,
        IEnergyDataCollectionService energyService,
        IDeviceMappingService deviceMapping,
        ILogger<DeviceData> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Live TotalEnergyFlow requested for DeviceId {DeviceId}", deviceId);

        // Map internal deviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await deviceMapping.GetThingsBoardDeviceIdAsync(deviceId, ct);
        if (thingsBoardDeviceId == null)
        {
            logger.LogWarning("Device mapping not found for DeviceId {DeviceId}", deviceId);
            return Results.Problem(
                detail: $"Device not found: {deviceId}",
                statusCode: StatusCodes.Status404NotFound);
        }

        var result = await energyService.FetchLifetimeEnergyViaRpcAsync(thingsBoardDeviceId.Value, ct);

        if (result == null)
        {
            logger.LogWarning("TotalEnergyFlow could not be retrieved for DeviceId {DeviceId} (TB: {ThingsBoardDeviceId})",
                deviceId, thingsBoardDeviceId);
            return Results.Problem(
                detail: "TotalEnergyFlow could not be retrieved (RPC failed or device offline).",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(result);
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
    ITelemetryDeduplicationService deduplicationService,
    IDeviceActivityService activityService)
{
    return await ProcessTelemetry(body, "BatterySoc", logger, sender, deduplicationService, activityService, TelemetrySource.BatterySoc);
}

public async Task<IResult> ReceivePowerFlowTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService,
    IDeviceActivityService activityService)
{
    return await ProcessPowerFlowTelemetry(body, DataTypes.PowerFlow, logger, sender, deduplicationService, activityService);
}

public async Task<IResult> ReceiveDailyEnergyFlowTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService,
    IDeviceActivityService activityService)
{
    return await ProcessEnergyFlowTelemetry(body, DataTypes.DailyCumulativeEnergy, logger, sender, deduplicationService, activityService, TelemetrySource.DailyEnergyFlow);
}

public async Task<IResult> ReceiveTotalEnergyFlowTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService,
    IDeviceActivityService activityService)
{
    return await ProcessEnergyFlowTelemetry(body, DataTypes.LifetimeEnergy, logger, sender, deduplicationService, activityService, TelemetrySource.TotalEnergyFlow);
}

private async Task<IResult> ProcessEnergyFlowTelemetry(
    JsonElement body,
    string dataType,
    ILogger<DeviceData> logger,
    ISender sender,
    ITelemetryDeduplicationService deduplicationService,
    IDeviceActivityService activityService,
    TelemetrySource telemetrySource)
{
    try
    {
        logger.LogInformation(
            "{DataType} energy flow telemetry received at {UtcNow}",
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

        // Record device activity (updates heartbeat cache, sends SignalR if status changed)
        await activityService.RecordActivityAsync(deviceId, telemetrySource, dataDateTime);

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
            PvEnergyKWhToday = isDailyData ? pvTotalEnergy : null,
            GridImportEnergyKWhToday = isDailyData ? gridImportEnergy : null,
            GridExportEnergyKWhToday = isDailyData ? gridExportEnergy : null,
            BatteryChargeEnergyKWhToday = isDailyData ? batteryChargeEnergy : null,
            BatteryDischargeEnergyKWhToday = isDailyData ? batteryDischargeEnergy : null,
            LoadEnergyKWhToday = isDailyData ? loadEnergy : null,
            GeneratorEnergyKWhToday = isDailyData ? generatorEnergy : null,

            // Total fields - only populated for Lifetime data
            PvEnergyKWhTotal = !isDailyData ? pvTotalEnergy : null,
            GridImportEnergyKWhTotal = !isDailyData ? gridImportEnergy : null,
            GridExportEnergyKWhTotal = !isDailyData ? gridExportEnergy : null,
            BatteryChargeEnergyKWhTotal = !isDailyData ? batteryChargeEnergy : null,
            BatteryDischargeEnergyKWhTotal = !isDailyData ? batteryDischargeEnergy : null,
            LoadEnergyKWhTotal = !isDailyData ? loadEnergy : null,
            GeneratorEnergyKWhTotal = !isDailyData ? generatorEnergy : null,

            // MPPT values (same for both - they're always the same granularity as the data type)
            PvMppt1EnergyKWh = pvMppt1Energy,
            PvMppt2EnergyKWh = pvMppt2Energy,
            PvMppt3EnergyKWh = pvMppt3Energy,
            PvMppt4EnergyKWh = pvMppt4Energy,

            CreatedBy = createdBy
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
    ITelemetryDeduplicationService deduplicationService,
    IDeviceActivityService activityService,
    TelemetrySource telemetrySource)
{
    try
    {
        logger.LogInformation(
            "{DataType} telemetry received at {UtcNow}",
            dataType,
            DateTime.UtcNow);

        // 1) Get the inner "data" object
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            logger.LogWarning("Received telemetry without 'data' object");
            return Results.Problem("Missing 'data' object", statusCode: 400);
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
                logger.LogWarning("Could not parse timestampUtc: {TimestampUtc}", tsStr);
            }
        }

        // 3) ThingsBoardDeviceId comes from root level "deviceId" field (string -> Guid)
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

        // Record device activity (updates heartbeat cache, sends SignalR if status changed)
        await activityService.RecordActivityAsync(deviceId, telemetrySource, requestDate);

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
    ITelemetryDeduplicationService deduplicationService,
    IDeviceActivityService activityService)
{
    try
    {
        logger.LogInformation(
            "{DataType} power flow telemetry received at {UtcNow}",
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

        // Record device activity (updates heartbeat cache, sends SignalR if status changed)
        await activityService.RecordActivityAsync(deviceId, TelemetrySource.PowerFlow, dataDateTime);

        // 4) Check for duplicate telemetry
        var shouldProcess = await deduplicationService.ShouldProcessTelemetryAsync(deviceId, dataType, dataDateTime);
        if (!shouldProcess)
        {
            logger.LogInformation(
                "Skipping duplicate {DataType} telemetry for device {DeviceId} at {Timestamp}",
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
            GeneratorTotalPowerW = generatorTotalPowerW,
            CreatedBy = createdBy
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

/// <summary>
/// Receives alarm telemetry from ThingsBoard (InverterAlarmState)
/// Accepts TB wrapper format: { deviceId, data: { ...InverterAlarmState } }
/// Converts TB ID to internal ID and processes alarms
/// </summary>
public async Task<IResult> ReceiveAlarmTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender,
    IDeviceMappingService deviceMapping,
    IDeviceNotificationService notificationService,
    IApplicationDbContext context,
    IDeviceActivityService activityService)
{
    try
    {
        logger.LogInformation("Alarm telemetry received at {UtcNow}", DateTime.UtcNow);

        // 1) Parse ThingsBoardDeviceId from root level "deviceId" field
        Guid thingsBoardDeviceId = Guid.Empty;
        if (body.TryGetProperty("deviceId", out var deviceIdElement))
        {
            var deviceIdStr = deviceIdElement.GetString();
            if (!string.IsNullOrWhiteSpace(deviceIdStr) && Guid.TryParse(deviceIdStr, out var parsedDeviceId))
            {
                thingsBoardDeviceId = parsedDeviceId;
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

        // Record device activity (updates heartbeat cache, sends SignalR if status changed)
        await activityService.RecordActivityAsync(thingsBoardDeviceId, TelemetrySource.Alarm);

        // 2) Convert ThingsBoard ID to internal device ID
        var internalDeviceId = await deviceMapping.GetDeviceIdAsync(thingsBoardDeviceId);
        if (internalDeviceId == null)
        {
            logger.LogWarning("No device found for ThingsBoardDeviceId: {ThingsBoardDeviceId}", thingsBoardDeviceId);
            return Results.Problem($"Device not found for ThingsBoardDeviceId: {thingsBoardDeviceId}", statusCode: 404);
        }

        // 3) Get the inner "data" object containing InverterAlarmState
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            logger.LogWarning("Received alarm telemetry without 'data' object");
            return Results.Problem("Missing 'data' object", statusCode: 400);
        }

        // 4) Parse InverterAlarmState from data
        var alarmState = new InverterAlarmStateDto();

        if (dataElement.TryGetProperty("timestampUtc", out var tsEl))
            alarmState.TimestampUtc = tsEl.GetString() ?? string.Empty;

        if (dataElement.TryGetProperty("gatewayId", out var gwEl))
            alarmState.GatewayId = gwEl.GetString() ?? string.Empty;

        if (dataElement.TryGetProperty("inverterSerialNumber", out var snEl))
            alarmState.InverterSerialNumber = snEl.GetString() ?? string.Empty;

        if (dataElement.TryGetProperty("runStateRaw", out var rsEl))
            alarmState.RunStateRaw = rsEl.GetInt32();

        if (dataElement.TryGetProperty("warningWord1Raw", out var ww1El))
            alarmState.WarningWord1Raw = ww1El.GetInt32();

        if (dataElement.TryGetProperty("warningWord2Raw", out var ww2El))
            alarmState.WarningWord2Raw = ww2El.GetInt32();

        if (dataElement.TryGetProperty("faultWord1Raw", out var fw1El))
            alarmState.FaultWord1Raw = fw1El.GetInt32();

        if (dataElement.TryGetProperty("faultWord2Raw", out var fw2El))
            alarmState.FaultWord2Raw = fw2El.GetInt32();

        if (dataElement.TryGetProperty("faultWord3Raw", out var fw3El))
            alarmState.FaultWord3Raw = fw3El.GetInt32();

        if (dataElement.TryGetProperty("faultWord4Raw", out var fw4El))
            alarmState.FaultWord4Raw = fw4El.GetInt32();

        // Parse activeAlarms array
        if (dataElement.TryGetProperty("activeAlarms", out var alarmsEl) &&
            alarmsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var alarmEl in alarmsEl.EnumerateArray())
            {
                var alarm = new AlarmDto();

                if (alarmEl.TryGetProperty("code", out var codeEl))
                    alarm.Code = codeEl.GetInt32();

                if (alarmEl.TryGetProperty("vendorCode", out var vcEl))
                    alarm.VendorCode = vcEl.GetInt32();

                if (alarmEl.TryGetProperty("severity", out var sevEl))
                    alarm.Severity = sevEl.GetString() ?? string.Empty;

                if (alarmEl.TryGetProperty("sourceDevice", out var sdEl))
                    alarm.SourceDevice = sdEl.GetString() ?? string.Empty;

                if (alarmEl.TryGetProperty("category", out var catEl))
                    alarm.Category = catEl.GetString() ?? string.Empty;

                if (alarmEl.TryGetProperty("messageKey", out var mkEl))
                    alarm.MessageKey = mkEl.GetString() ?? string.Empty;

                if (alarmEl.TryGetProperty("messageParams", out var mpEl) &&
                    mpEl.ValueKind == JsonValueKind.Object)
                {
                    alarm.MessageParams = JsonSerializer.Deserialize<Dictionary<string, object>>(mpEl.GetRawText())
                        ?? new Dictionary<string, object>();
                }

                if (alarmEl.TryGetProperty("firstOccurredUtc", out var foEl))
                    alarm.FirstOccurredUtc = foEl.GetString() ?? string.Empty;

                if (alarmEl.TryGetProperty("lastOccurredUtc", out var loEl))
                    alarm.LastOccurredUtc = loEl.GetString() ?? string.Empty;

                if (alarmEl.TryGetProperty("occurrenceCount", out var ocEl))
                    alarm.OccurrenceCount = ocEl.GetInt32();

                alarmState.ActiveAlarms.Add(alarm);
            }
        }

        // 5) Create command with internal device ID
        var command = new ProcessDeviceAlarmCommand
        {
            DeviceId = internalDeviceId.Value,
            AlarmState = alarmState
        };

        var result = await sender.Send(command);

        // 6) Send SignalR notifications for created notifications
        if (result.CreatedNotificationIds.Any())
        {
            // Create a lookup dictionary from the processed notifications for quick access
            var processedNotificationLookup = result.Notifications.ToDictionary(n => n.Id);

            // Fetch created notifications to send via SignalR
            var notifications = await context.DeviceNotifications
                .Include(dn => dn.DeviceNotificationType)
                .Include(dn => dn.Device)
                .Include(dn => dn.Site)
                .Where(dn => result.CreatedNotificationIds.Contains(dn.Id))
                .ToListAsync();

            // Send notifications via SignalR for critical severity levels
            // Severity values: ALARM, ERROR, WARNING
            var criticalSeverities = new[] { "ALARM", "ERROR", "WARNING" };

            foreach (var dn in notifications)
            {
                if (criticalSeverities.Contains(dn.DeviceNotificationType.Severity))
                {
                    // Get the processed notification info for additional details
                    var processedInfo = processedNotificationLookup.GetValueOrDefault(dn.Id);

                    var signalRNotification = new DeviceNotificationDto
                    {
                        Id = dn.Id,
                        DeviceId = dn.DeviceId,
                        SiteId = dn.SiteId,
                        SetupDeviceId = dn.SetupDeviceId,
                        DeviceNotificationTypeId = dn.DeviceNotificationTypeId,
                        Timestamp = dn.Timestamp,
                        AdditionalData = dn.AdditionalData,
                        IsProcessed = dn.IsProcessed,
                        Created = dn.Created,
                        DeviceNotificationType = new DeviceNotificationTypeDto
                        {
                            Id = dn.DeviceNotificationType.Id,
                            Code = dn.DeviceNotificationType.Code,
                            Message = dn.DeviceNotificationType.Message,
                            Status = dn.DeviceNotificationType.Status,
                            Description = dn.DeviceNotificationType.Description,
                            Register = dn.DeviceNotificationType.Register,
                            Value = dn.DeviceNotificationType.Value,
                            Category = dn.DeviceNotificationType.Category,
                            Severity = dn.DeviceNotificationType.Severity,
                            Version = dn.DeviceNotificationType.Version,
                            Manufacturer = dn.DeviceNotificationType.Manufacturer,
                            IsActive = dn.DeviceNotificationType.IsActive
                        },
                        DeviceManufacturer = dn.Device.Manufacturer,
                        SiteName = dn.Site.Name,
                        DeviceName = dn.Device.Name,
                        // Populate display properties from processed notification info
                        Key = processedInfo?.Key ?? dn.DeviceNotificationType.Code,
                        DisplayMessage = processedInfo?.Message ?? dn.DeviceNotificationType.Message,
                        Severity = processedInfo?.Severity ?? dn.DeviceNotificationType.Severity,
                        DetailedDescription = processedInfo?.Description ?? dn.DeviceNotificationType.Description
                    };

                    await notificationService.SendNotificationToSiteAsync(dn.SiteId, signalRNotification);
                    await notificationService.SendNotificationToDeviceAsync(dn.DeviceId, signalRNotification);
                }
            }
        }

        logger.LogInformation(
            "Alarm telemetry processed for device {DeviceId} (TB: {ThingsBoardDeviceId}). " +
            "Processed: {ProcessedCount}, Created: {CreatedCount}, Errors: {ErrorCount}",
            internalDeviceId, thingsBoardDeviceId,
            result.ProcessedAlarmsCount, result.CreatedNotificationIds.Count, result.Errors.Count);

        return Results.Ok(new
        {
            success = result.Errors.Count == 0,
            processedCount = result.ProcessedAlarmsCount,
            createdCount = result.CreatedNotificationIds.Count,
            notifications = result.Notifications,
            errors = result.Errors
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling alarm telemetry");
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
