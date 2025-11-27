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
            .MapPost(ReceiveBatterySocTelemetry, "BatterySoc");

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
