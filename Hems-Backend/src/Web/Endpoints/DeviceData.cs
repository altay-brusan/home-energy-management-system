using System.Globalization;
using System.Text.Json;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceData.Commands.BulkCreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceEnergyData;
using Hems_Backend.Application.DeviceData.Queries.GetDeviceData;
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
            .MapPost(ReceiveTotalEnergyFlowTelemetry, "TotalEnergyFlow");

        // User endpoints (require user authorization)
        app.MapGroup(this)
            //.RequireAuthorization()
            .MapGet(GetDeviceData)
            .MapGet(GetLivePowerFlow, "PowerFlow/live/{deviceId:guid}"); ;
    }

    public async Task<IResult> GetLivePowerFlow(
    Guid deviceId,
    IPowerFlowService powerFlowService,
    ILogger<DeviceData> logger,
    CancellationToken ct)
    {
        logger.LogInformation("🔌 Live PowerFlow requested for ThingsBoardDeviceId {ThingsBoardDeviceId}", deviceId);

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
    ISender sender)
{
    return await ProcessTelemetry(body, "BatterySoc", logger, sender);
}

public async Task<IResult> ReceivePowerFlowTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender)
{
    return await ProcessTelemetry(body, "PowerFlow", logger, sender);
}

public async Task<IResult> ReceiveDailyEnergyFlowTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender)
{
    return await ProcessEnergyFlowTelemetry(body, DataTypes.DailyCumulativeEnergy, logger, sender);
}

public async Task<IResult> ReceiveTotalEnergyFlowTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender)
{
    return await ProcessEnergyFlowTelemetry(body, DataTypes.LifetimeEnergy, logger, sender);
}

private async Task<IResult> ProcessEnergyFlowTelemetry(
    JsonElement body,
    string dataType,
    ILogger<DeviceData> logger,
    ISender sender)
{
    try
    {
        logger.LogInformation(
            "📊 {DataType} energy flow telemetry received at {UtcNow}",
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

        // 3) Parse ThingsBoardDeviceId
        var deviceIdResult = TryParseDeviceId(dataElement, logger);
        if (deviceIdResult.Error != null)
        {
            return deviceIdResult.Error;
        }
        var deviceId = deviceIdResult.DeviceId;

        // 4) Parse energy values based on structure
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
    ISender sender)
{
    try
    {
        logger.LogInformation(
            "📥 {DataType} telemetry received at {UtcNow}",
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

        // 3) ThingsBoardDeviceId comes from data.ThingsBoardDeviceId (string → Guid)
        var deviceIdResult = TryParseDeviceId(dataElement, logger);
        if (deviceIdResult.Error != null)
        {
            return deviceIdResult.Error;
        }
        var deviceId = deviceIdResult.DeviceId;

        // 4) Store the whole inner data object as JsonDocument
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

private static (Guid DeviceId, IResult? Error) TryParseDeviceId(
        JsonElement dataElement,
        ILogger<DeviceData> logger)
    {
        if (!dataElement.TryGetProperty("ThingsBoardDeviceId", out var devIdEl))
        {
            return (Guid.Empty, null);
        }

        var devIdStr = devIdEl.GetString();
        if (string.IsNullOrWhiteSpace(devIdStr))
        {
            logger.LogWarning("ThingsBoardDeviceId is null or empty, using Guid.Empty");
            return (Guid.Empty, Results.Problem("Missing 'ThingsBoardDeviceId' in data", statusCode: 400));
        }

        if (!Guid.TryParse(devIdStr, out var deviceId))
        {
            logger.LogWarning("ThingsBoardDeviceId '{ThingsBoardDeviceId}' is not a valid Guid, using Guid.Empty", devIdStr);
            return (Guid.Empty, Results.Problem("Invalid 'ThingsBoardDeviceId' format", statusCode: 400));
        }

        return (deviceId, null);
    }

}
