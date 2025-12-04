using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceComponents.Queries.GetDeviceSummary;
using Hems_Backend.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hems_Backend.Web.Endpoints;

public class Components : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetDeviceSummary, "DeviceSummary");
    }

    [Authorize]
    public async Task<DeviceSummaryResponseDto> GetDeviceSummary(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetDeviceSummaryQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }
}
