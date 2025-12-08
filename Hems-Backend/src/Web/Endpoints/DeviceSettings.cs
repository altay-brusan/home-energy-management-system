using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceSettings.Commands.ChangeExportStrategy;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class DeviceSettings : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapPost(ChangeExportStrategy, "ChangeExportStrategy");
    }

    /// <summary>
    /// This endpoint allows changing the export strategy of a device.
    /// It is for demonstration purposes only and may be removed in future releases.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    [Authorize]
    public async Task<IResult> ChangeExportStrategy(
        ISender sender,
        ChangeExportStrategyCommand command)
    {
        var response = await sender.Send(command);

        if (response != null && response.Success)
        {
            return Results.Ok(response);
        }

        if (response != null)
        {
            // Device responded but with failure
            return Results.BadRequest(response);
        }

        // No response from device (timeout, offline, etc.)
        return Results.BadRequest(new
        {
            success = false,
            message = "Failed to communicate with device. Device may be offline or timed out."
        });
    }
}
