using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceData.Commands.BulkCreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;
using Hems_Backend.Application.DeviceData.Queries.GetDeviceData;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hems_Backend.Web.Endpoints;

public class DeviceData : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Device endpoints (require device-only authorization)
        app.MapGroup(this)
            .RequireAuthorization(Policies.DeviceOnly)
            .MapPost(CreateDeviceData)
            .MapPost("bulk", BulkCreateDeviceData);

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
}
