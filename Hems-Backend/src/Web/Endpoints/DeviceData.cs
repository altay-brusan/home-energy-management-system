using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceData.Commands.BulkCreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceEnergyData;
using Hems_Backend.Application.DeviceData.Commands.ProcessAlarmTelemetry;
using Hems_Backend.Application.DeviceData.Commands.ProcessEnergyFlowTelemetry;
using Hems_Backend.Application.DeviceData.Commands.ProcessGenericTelemetry;
using Hems_Backend.Application.DeviceData.Commands.ProcessGridStatusTelemetry;
using Hems_Backend.Application.DeviceData.Commands.ProcessPowerFlowTelemetry;
using Hems_Backend.Application.DeviceData.Common.Models;
using Hems_Backend.Application.DeviceData.Queries.GetDeviceData;
using Hems_Backend.Application.DeviceData.Queries.GetDeviceEnergyData;
using Hems_Backend.Application.Devices.Queries.GetDeviceHeartbeatStatus;
using Hems_Backend.Application.Devices.Queries.GetDemoDiagnostics;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Infrastructure.Services;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
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
            .MapPost(ReceiveHeartbeat, "Heartbeat")
            .MapPost(ReceiveAlarmTelemetry, "Alarm")
            .MapPost(ReceiveGridStatusTelemetry, "GridStatus");

        // User endpoints (require user authorization)
        app.MapGroup(this)
            .MapGet(GetDeviceData)
            .MapGet(GetLivePowerFlow, "PowerFlow/live/{deviceId:guid}")
            .MapGet(GetDeviceHeartbeatStatus, "Status/{deviceId:guid}")
            .MapGet(GetLiveDailyEnergyFlow, "DailyEnergyFlow/live/{deviceId:guid}")
            .MapGet(GetLiveTotalEnergyFlow, "TotalEnergyFlow/live/{deviceId:guid}")
            .MapGet(GetDemoDeviceDiagnostics, "Demo/Diagnostics");
    }

    #region Telemetry Receive Endpoints

    /// <summary>
    /// Receives lightweight heartbeat signal from devices (every 1 minute)
    /// </summary>
    public async Task<IResult> ReceiveHeartbeat(
        HeartbeatDto request,
        IDeviceActivityService activityService,
        ILogger<DeviceData> logger)
    {
        try
        {
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

    /// <summary>
    /// Receives power flow telemetry from ThingsBoard
    /// </summary>
    public async Task<IResult> ReceivePowerFlowTelemetry(
        [FromBody] JsonElement body,
        ISender sender)
    {
        var result = await sender.Send(new ProcessPowerFlowTelemetryCommand { RawBody = body });

        if (!result.Success)
        {
            return Results.Problem(result.Error, statusCode: 400);
        }

        return Results.Ok(new
        {
            message = result.Message,
            id = result.CreatedId,
            deviceId = result.DeviceId,
            dataDateTime = result.DataDateTime,
            duplicate = result.IsDuplicate
        });
    }

    /// <summary>
    /// Receives battery SOC telemetry from ThingsBoard
    /// </summary>
    public async Task<IResult> ReceiveBatterySocTelemetry(
        [FromBody] JsonElement body,
        ISender sender)
    {
        var result = await sender.Send(new ProcessGenericTelemetryCommand
        {
            RawBody = body,
            DataType = "BatterySoc",
            TelemetrySource = TelemetrySource.BatterySoc
        });

        if (!result.Success)
        {
            return Results.Problem(result.Error, statusCode: 400);
        }

        return Results.Ok(new
        {
            message = result.Message,
            id = result.CreatedId,
            duplicate = result.IsDuplicate
        });
    }

    /// <summary>
    /// Receives daily energy flow telemetry from ThingsBoard
    /// </summary>
    public async Task<IResult> ReceiveDailyEnergyFlowTelemetry(
        [FromBody] JsonElement body,
        ISender sender)
    {
        var result = await sender.Send(new ProcessEnergyFlowTelemetryCommand
        {
            RawBody = body,
            DataType = DataTypes.DailyCumulativeEnergy,
            TelemetrySource = TelemetrySource.DailyEnergyFlow
        });

        if (!result.Success)
        {
            return Results.Problem(result.Error, statusCode: 400);
        }

        return Results.Ok(new
        {
            message = result.Message,
            id = result.CreatedId,
            deviceId = result.DeviceId,
            dataDateTime = result.DataDateTime,
            duplicate = result.IsDuplicate
        });
    }

    /// <summary>
    /// Receives total (lifetime) energy flow telemetry from ThingsBoard
    /// </summary>
    public async Task<IResult> ReceiveTotalEnergyFlowTelemetry(
        [FromBody] JsonElement body,
        ISender sender)
    {
        var result = await sender.Send(new ProcessEnergyFlowTelemetryCommand
        {
            RawBody = body,
            DataType = DataTypes.LifetimeEnergy,
            TelemetrySource = TelemetrySource.TotalEnergyFlow
        });

        if (!result.Success)
        {
            return Results.Problem(result.Error, statusCode: 400);
        }

        return Results.Ok(new
        {
            message = result.Message,
            id = result.CreatedId,
            deviceId = result.DeviceId,
            dataDateTime = result.DataDateTime,
            duplicate = result.IsDuplicate
        });
    }

    /// <summary>
    /// Receives alarm telemetry from ThingsBoard (InverterAlarmState)
    /// </summary>
    public async Task<IResult> ReceiveAlarmTelemetry(
        [FromBody] JsonElement body,
        ISender sender)
    {
        var result = await sender.Send(new ProcessAlarmTelemetryCommand { RawBody = body });

        if (!result.Success && result.Errors.Any())
        {
            return Results.Problem(result.Errors.First(), statusCode: 400);
        }

        return Results.Ok(new
        {
            success = result.Success,
            processedCount = result.ProcessedCount,
            createdCount = result.CreatedCount,
            notifications = result.Notifications,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Receives grid status telemetry from device (phase voltages and grid connected state)
    /// </summary>
    public async Task<IResult> ReceiveGridStatusTelemetry(
        [FromBody] JsonElement body,
        ISender sender)
    {
        var result = await sender.Send(new ProcessGridStatusTelemetryCommand { RawBody = body });

        if (!result.Success)
        {
            return Results.Problem(result.Error, statusCode: 400);
        }

        return Results.Ok(new
        {
            message = result.Message,
            deviceId = result.DeviceId,
            gridConnected = result.GridConnected,
            statusChanged = result.StatusChanged,
            notificationSent = result.NotificationSent
        });
    }

    #endregion

    #region Query Endpoints

    public async Task<PaginatedList<DeviceDataDto>> GetDeviceData(
        ISender sender,
        [AsParameters] GetDeviceDataQuery query)
    {
        return await sender.Send(query);
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

    /// <summary>
    /// Gets live power flow data from device via RPC
    /// </summary>
    public async Task<IResult> GetLivePowerFlow(
        Guid deviceId,
        IPowerFlowService powerFlowService,
        ILogger<DeviceData> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Live PowerFlow requested for ThingsBoardDeviceId {ThingsBoardDeviceId}", deviceId);

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
    /// Gets live daily energy data from device via RPC
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
    /// Gets live total (lifetime) energy data from device via RPC
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

    /// <summary>
    /// Diagnostics endpoint to check demo device configuration
    /// FOR TEST ONLY - DO NOT USE IN PRODUCTION
    /// </summary>
    public async Task<IResult> GetDemoDeviceDiagnostics(
        ISender sender,
        ILogger<DeviceData> logger)
    {
        try
        {
            var result = await sender.Send(new GetDemoDiagnosticsQuery());
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting demo diagnostics");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    #endregion

    #region Legacy Device-Only Endpoints (kept for backward compatibility)

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

    #endregion
}
