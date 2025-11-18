using System.Globalization;
using System.Text.Json;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceData.Commands.BulkCreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceEnergyData;
using Hems_Backend.Application.DeviceData.Queries.GetDeviceData;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // make sure this is present
using Microsoft.AspNetCore.Mvc;

namespace Hems_Backend.Web.Endpoints;

public class DeviceData : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Device endpoints (require device-only authorization)
        //app.MapGroup(this)
        //    .RequireAuthorization(Policies.DeviceOnly)
        //    .MapPost(CreateDeviceData)
        //    .MapPost(CreateDeviceEnergyData, "energy")
        //    .MapPost("bulk", BulkCreateDeviceData);

        // User endpoints (require user authorization)
        app.MapGroup(this)
            .MapPost(ReceiveTelemetry, "PowerFlow");

        // User endpoints (require user authorization)
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetDeviceData);
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


public async Task<IResult> ReceiveTelemetry(
    [FromBody] JsonElement body,
    ILogger<DeviceData> logger,
    ISender sender)
{
    try
    {
        logger.LogInformation(
            "📥 ThingsBoard telemetry received at {UtcNow}: {Payload}",
            DateTime.UtcNow,
            body.ToString());

        // 1) Get the inner "data" object
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            logger.LogWarning("Received telemetry without 'data' object");
            return Results.Problem("Missing 'data' object", statusCode: 400);
        }

        logger.LogInformation("📦 ThingsBoard inner data payload: {InnerPayload}", dataElement.ToString());

        // 2) DataType comes from data.kind
        var dataType = dataElement.TryGetProperty("kind", out var kindEl)
            ? kindEl.GetString() ?? string.Empty
            : string.Empty;

        // 3) RequestDate comes from data.timestampUtc
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

            // 4) DeviceId comes from data.DeviceId (string → Guid)
            Guid deviceId = Guid.Empty;
            if (dataElement.TryGetProperty("DeviceId", out var devIdEl))
            {
                var devIdStr = devIdEl.GetString();
                if (!string.IsNullOrWhiteSpace(devIdStr))
                {
                    if (!Guid.TryParse(devIdStr, out deviceId))
                    {
                        logger.LogWarning("DeviceId '{DeviceId}' is not a valid Guid, using Guid.Empty", devIdStr);
                        return Results.Problem("Invalid 'DeviceId' format", statusCode: 400);
                    }
                }
                else
                {
                    logger.LogWarning("DeviceId is null or empty, using Guid.Empty");
                    return Results.Problem("Missing 'DeviceId' in data", statusCode: 400);
                }
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
            message = "Telemetry received",
            id = createdId
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling ThingsBoard telemetry");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
}



}
